"""
04_transform_to_ef.py
Transform staging tables (stg_*) → EF Core PostgreSQL schema.
Run AFTER: 02_migrate.py  (loads stg_* from SQL Server)
"""

import psycopg2
from psycopg2.extras import execute_batch
from psycopg2.extensions import register_adapter, AsIs
import psycopg2.extras
import uuid

# Register UUID adapter so psycopg2 can handle uuid.UUID objects natively
psycopg2.extras.register_uuid()
from datetime import datetime, timezone
import sys

PG = dict(host="localhost", port=5432, dbname="flight_api",
          user="postgres", password="123456")
NOW = datetime.now(timezone.utc)
BATCH = 500


# ── helpers ────────────────────────────────────────────────────────────────────

def pg():
    return psycopg2.connect(**PG)

def ok(tbl, n):
    print(f"  ✓ {tbl:<45} {n:>10,} rows")

def skip(tbl, reason="stg table not found"):
    print(f"  - {tbl:<45} SKIP ({reason})")

def table_exists(cur, name):
    cur.execute("SELECT 1 FROM information_schema.tables WHERE table_name=%s", (name,))
    return cur.fetchone() is not None

def trunc(v, n):
    if v is None: return None
    return str(v)[:n]

def safe_str(v, n, default=''):
    if v is None: return default
    return str(v)[:n]

def safe_bool(v):
    if isinstance(v, bool): return v
    if v is None: return False
    return bool(v)

def safe_float(v, default=0.0):
    if v is None: return default
    try: return float(v)
    except: return default

def safe_int(v, default=0):
    if v is None: return default
    try: return int(v)
    except: return default

def map_source(ibe):
    if not ibe: return None
    return {"galileo":"Galileo","datacom":"Datacom","kiwi":"Kiwi",
            "pkfare":"Pkfare","maybay":"Maybay"}.get(str(ibe).lower(),
            str(ibe).capitalize())

def map_trip_type(n):
    try: return {1:"OneWay", 2:"RoundTrip"}.get(int(n), "OneWay")
    except: return "OneWay"

def map_status(s):
    if not s: return "Pending"
    return {"pending":"Pending","confirmed":"Confirmed","ticketed":"Ticketed",
            "cancelled":"Cancelled","expired":"Expired","failed":"Failed"
            }.get(str(s).lower(), "Pending")

def map_pax_type(code):
    return {"ADT":"Adult","CHD":"Child","INF":"Infant"}.get(
        str(code) if code else "", "Adult")


# ── main ───────────────────────────────────────────────────────────────────────

def main():
    conn = pg()
    cur  = conn.cursor()

    cur.execute("SELECT COUNT(*) FROM information_schema.tables WHERE table_name LIKE 'stg_%'")
    stg_count = cur.fetchone()[0]
    if stg_count == 0:
        print("ERROR: No staging tables (stg_*) found. Run 02_migrate.py first.")
        sys.exit(1)
    print(f"\nFound {stg_count} staging tables. Starting EF transform...\n")

    # Truncate all EF tables so script is idempotent (safe to re-run)
    print("  Clearing all EF tables for fresh insert...")
    cur.execute("""
        TRUNCATE TABLE
            trip_cancellations, trip_tours, trip_visas, baggages, insurances,
            invoices, car_rentals, tickets, passengers,
            booking_segments, booking_flights, bookings,
            class_and_notes, airline_ignores, lcc_infos,
            agent_partners, agent_pccs, partners,
            search_analytics, user_accounts, user_roles,
            agents, pccs,
            geo_airports, geo_cities, geo_countries, geo_continents,
            passenger_types, airline_types, aircrafts, airlines
        RESTART IDENTITY CASCADE
    """)
    conn.commit()
    print("  Done.\n")

    # ── 1. AIRLINE TYPES ──────────────────────────────────────────────────────
    cur.execute('SELECT "Type", "Descr" FROM stg_airline_types')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO airline_types (code, name, visible, created_on_utc)
        VALUES (%s,%s,true,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], NOW) for r in rows], page_size=BATCH)
    conn.commit(); ok("airline_types", len(rows))

    # ── 2. AIRLINES ───────────────────────────────────────────────────────────
    cur.execute('SELECT "Code","Name_Vi","Name_En","Logo","Visible" FROM stg_airlines')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO airlines (code, name, logo, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1] or r[2], trunc(r[3],500), safe_bool(r[4]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("airlines", len(rows))

    # ── 3. AIRCRAFTS ──────────────────────────────────────────────────────────
    cur.execute('SELECT "IATA","Manufacturer","Model","Visible" FROM stg_aircrafts')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO aircrafts (iata, manufacturer, model, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], trunc(r[1],100), trunc(r[2],100), safe_bool(r[3]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("aircrafts", len(rows))

    # ── 4. PASSENGER TYPES ────────────────────────────────────────────────────
    if table_exists(cur, "stg_passenger_types"):
        cur.execute('SELECT "Code","Name_Vi","Name_En","Description" FROM stg_passenger_types')
        rows = cur.fetchall()
        execute_batch(conn.cursor(), """
            INSERT INTO passenger_types (code, name_vi, name_en, description)
            VALUES (%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, [(r[0], r[1], r[2], r[3]) for r in rows], page_size=BATCH)
        conn.commit(); ok("passenger_types", len(rows))

    # ── 5. GEO CONTINENTS ─────────────────────────────────────────────────────
    # NOTE: PK column is "Id" (capital I, text)
    cur.execute('SELECT "Code","Name_Vi","Name_En","Name_Fr","Visible" FROM stg_geo_continents')
    rows = cur.fetchall()
    if table_exists(cur, "stg_baggage_continents"):
        cur.execute('SELECT "Code","Name_Vi","Name_En","Name_Fr","Visible" FROM stg_baggage_continents')
        rows += cur.fetchall()
    seen = set()
    uniq = [r for r in rows if r[0] not in seen and not seen.add(r[0])]
    execute_batch(conn.cursor(), """
        INSERT INTO geo_continents ("Id", name_vi, name_en, name_fr, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1] or '', r[2] or '', r[3] or '', safe_bool(r[4]), NOW)
          for r in uniq], page_size=BATCH)
    conn.commit(); ok("geo_continents", len(uniq))

    # ── 6. GEO COUNTRIES ──────────────────────────────────────────────────────
    # FK column: continent_code (refs geo_continents.Id)
    cur.execute('SELECT "Code","ContinentCode","Name_Vi","Name_En","Name_Fr","Flag","Visible" FROM stg_geo_countries')
    rows = cur.fetchall()
    if table_exists(cur, "stg_baggage_countries"):
        cur.execute('SELECT "Code","ContinentCode","Name_Vi","Name_En","Name_Fr","Flag","Visible" FROM stg_baggage_countries')
        rows += cur.fetchall()
    seen = set()
    uniq = [r for r in rows if r[0] not in seen and not seen.add(r[0])]
    execute_batch(conn.cursor(), """
        INSERT INTO geo_countries ("Id", continent_code, name_vi, name_en, name_fr, flag, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], r[2] or '', r[3] or '', r[4] or '', trunc(r[5],150), safe_bool(r[6]), NOW)
          for r in uniq], page_size=BATCH)
    conn.commit(); ok("geo_countries", len(uniq))

    # ── 7. GEO CITIES ─────────────────────────────────────────────────────────
    cur.execute('SELECT "Code","CountryCode","Name_Vi","Name_En","Name_Fr","Location","SearchKeys","Visible" FROM stg_geo_cities')
    rows = cur.fetchall()
    if table_exists(cur, "stg_baggage_cities"):
        cur.execute('SELECT "Code","CountryCode","Name_Vi","Name_En","Name_Fr","Location",NULL,"Visible" FROM stg_baggage_cities')
        rows += cur.fetchall()
    seen = set()
    uniq = [r for r in rows if r[0] not in seen and not seen.add(r[0])]
    execute_batch(conn.cursor(), """
        INSERT INTO geo_cities ("Id", country_code, name_vi, name_en, name_fr, location, search_keys, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], r[2] or '', r[3] or '', r[4] or '', trunc(r[5],50), r[6], safe_bool(r[7]), NOW)
          for r in uniq], page_size=BATCH)
    conn.commit(); ok("geo_cities", len(uniq))

    # ── 8. GEO AIRPORTS ───────────────────────────────────────────────────────
    cur.execute('SELECT "Code","CityCode","Name_Vi","Name_En","Name_Fr","Location","SearchKeys","Visible" FROM stg_geo_airports')
    rows = cur.fetchall()
    if table_exists(cur, "stg_baggage_airports"):
        cur.execute('SELECT "Code","CityCode","Name_Vi","Name_En","Name_Fr","Location",NULL,"Visible" FROM stg_baggage_airports')
        rows += cur.fetchall()
    seen = set()
    uniq = [r for r in rows if r[0] not in seen and not seen.add(r[0])]
    execute_batch(conn.cursor(), """
        INSERT INTO geo_airports ("Id", city_code, name_vi, name_en, name_fr, location, search_keys, visible, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], r[2] or '', r[3] or '', r[4] or '', trunc(r[5],50), r[6], safe_bool(r[7]), NOW)
          for r in uniq], page_size=BATCH)
    conn.commit(); ok("geo_airports", len(uniq))

    # ── 9. PCCS ───────────────────────────────────────────────────────────────
    cur.execute('SELECT "Pcc","Active" FROM stg_pccs')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO pccs ("Id", active, created_on_utc)
        VALUES (%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], safe_bool(r[1]), NOW) for r in rows], page_size=BATCH)
    conn.commit(); ok("pccs", len(rows))

    # ── 10. USER ROLES ────────────────────────────────────────────────────────
    cur.execute('SELECT "Id","Name","Description" FROM stg_user_roles')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO user_roles ("Id", name, description, created_on_utc)
        VALUES (%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], r[2], NOW) for r in rows], page_size=BATCH)
    conn.commit(); ok("user_roles", len(rows))

    # ── 11. USER ACCOUNTS ─────────────────────────────────────────────────────
    # NOTE: PK column is "Id" (capital I, generated identity for API users)
    cur.execute("""
        SELECT "Id","UserRoleId","Email","Password","Phone","FullName",
               "Gender","Address","Avatar","CreateDate","LastLoginDate",
               "IPLastLogin","Active","Visible","OTP"
        FROM stg_users_website
    """)
    website = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO user_accounts
            ("Id", user_role_id, email, password, phone, full_name,
             gender, address, avatar, create_date, last_login_date,
             ip_last_login, active, visible, otp, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
        ON CONFLICT DO NOTHING
    """, [(r[0], r[1], safe_str(r[2],150,'unknown@unknown.com'),
           safe_str(r[3],200,'unknown'), trunc(r[4],20), trunc(r[5],200),
           r[6], trunc(r[7],500), trunc(r[8],500), r[9], r[10],
           trunc(r[11],50), safe_bool(r[12]), safe_bool(r[13]),
           trunc(r[14],10), NOW)
          for r in website], page_size=BATCH)
    conn.commit()
    # Reset sequence before API users
    conn.cursor().execute(
        "SELECT setval(pg_get_serial_sequence('user_accounts','Id'), "
        "COALESCE((SELECT MAX(\"Id\") FROM user_accounts),0))")
    conn.commit()
    if table_exists(cur, "stg_users_api"):
        cur.execute("""
            SELECT "UserRoleId","Email","Password","Phone","FullName",
                   "Gender","Address","Avatar","CreateDate","LastLoginDate",
                   "IPLastLogin","Active","Visible","OTP"
            FROM stg_users_api
        """)
        api = cur.fetchall()
        execute_batch(conn.cursor(), """
            INSERT INTO user_accounts
                (user_role_id, email, password, phone, full_name,
                 gender, address, avatar, create_date, last_login_date,
                 ip_last_login, active, visible, otp, created_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
            ON CONFLICT DO NOTHING
        """, [(r[0], safe_str(r[1],150,'unknown@api.com'),
               safe_str(r[2],200,'unknown'), trunc(r[3],20), trunc(r[4],200),
               r[5], trunc(r[6],500), trunc(r[7],500), r[8], r[9],
               trunc(r[10],50), safe_bool(r[11]), safe_bool(r[12]),
               trunc(r[13],10), NOW)
              for r in api], page_size=BATCH)
        conn.commit()
        ok("user_accounts", len(website) + len(api))
    else:
        ok("user_accounts", len(website))

    # ── 12. AGENTS ────────────────────────────────────────────────────────────
    cur.execute("""
        SELECT "Id","AgentCode","Name","Address","Tel","Email","Password",
               "Lcc_VN_Active_Domestic","Lcc_VN_Active_Global",
               "GalileoPcc","GalileoActive","DefaultCurrency",
               "EnableCache","CacheTime","SendMailInApi",
               COALESCE("EmailSender",0),"CombinedMode",
               "ExpiryDate","Active","BaggageFeePercent","BaggageFeeAmount",
               "CreateDate"
        FROM stg_agents
        WHERE "ExpiryDate" IS NOT NULL
    """)
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO agents
            (id, agent_code, name, address, tel, email, password_hash,
             lcc_vn_active_domestic, lcc_vn_active_global,
             galileo_pcc, galileo_active, default_currency,
             enable_cache, cache_time_minutes, send_mail_in_api,
             email_sender, combined_mode,
             expiry_date, active, baggage_fee_percent, baggage_fee_amount,
             created_at)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
        ON CONFLICT DO NOTHING
    """, [(r[0], safe_str(r[1],50,'UNK'), safe_str(r[2],150,'Unknown'),
           trunc(r[3],150), trunc(r[4],50),
           safe_str(r[5],150,'unknown@unknown.com'),
           safe_str(r[6],250,'unknown'),
           safe_bool(r[7]), safe_bool(r[8]),
           trunc(r[9],50), safe_bool(r[10]), trunc(r[11],10),
           safe_bool(r[12]), safe_int(r[13], 30), safe_bool(r[14]),
           safe_int(r[15]), safe_int(r[16]),
           r[17], safe_bool(r[18]),
           safe_float(r[19]), safe_float(r[20]),
           r[21] or NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("agents", len(rows))

    # Build agent_id → agent_code lookup
    cur.execute('SELECT "Id","AgentCode" FROM stg_agents')
    agent_code_map = {r[0]: r[1] for r in cur.fetchall()}
    # Build set of valid agent IDs in EF agents table
    cur.execute('SELECT id FROM agents')
    valid_agent_ids = {r[0] for r in cur.fetchall()}

    # ── 13. AGENT PCCS ────────────────────────────────────────────────────────
    cur.execute('SELECT "Id","AgentId","Pcc","IgnoredMode","ListStartPoint","Active" FROM stg_agent_pccs')
    rows = [r for r in cur.fetchall() if r[1] in valid_agent_ids]
    execute_batch(conn.cursor(), """
        INSERT INTO agent_pccs ("Id", agent_id, pcc, ignored_mode, list_start_point, active, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], trunc(r[2],10) or 'UNK', safe_int(r[3]), r[4], safe_bool(r[5]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("agent_pccs", len(rows))

    # ── 14. PARTNERS ──────────────────────────────────────────────────────────
    cur.execute('SELECT "Id","Name","Active" FROM stg_partners')
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO partners ("Id", name, active, created_on_utc)
        VALUES (%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], safe_bool(r[2]), NOW) for r in rows], page_size=BATCH)
    conn.commit(); ok("partners", len(rows))

    # ── 15. AGENT PARTNERS ────────────────────────────────────────────────────
    cur.execute('SELECT "Id","AgentId","PartnerId","IgnoredMode","ListStartPoint","Active" FROM stg_agent_partners')
    rows = [r for r in cur.fetchall() if r[1] in valid_agent_ids]
    execute_batch(conn.cursor(), """
        INSERT INTO agent_partners ("Id", agent_id, partner_id, ignored_mode, list_start_point, active, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], r[2], safe_int(r[3]), r[4], safe_bool(r[5]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("agent_partners", len(rows))

    # ── 16. LCC INFOS ─────────────────────────────────────────────────────────
    # NOTE: "Id" is generated identity — do NOT insert id
    cur.execute('SELECT "AgentId","Airline","AllowSearch","AllowBook" FROM stg_lcc_info')
    rows = [r for r in cur.fetchall() if r[0] in valid_agent_ids]
    execute_batch(conn.cursor(), """
        INSERT INTO lcc_infos (agent_id, airline, allow_search, allow_book, created_on_utc)
        VALUES (%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], trunc(r[1],10) or 'UNK', safe_bool(r[2]), safe_bool(r[3]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("lcc_infos", len(rows))

    # ── 17. AIRLINE IGNORES ───────────────────────────────────────────────────
    cur.execute('SELECT "Id","AgentId","Airline","FilterByPlatingCarrier","FilterByAnySegment","FilterByAllSegment" FROM stg_airline_ignores')
    rows = [r for r in cur.fetchall() if r[1] in valid_agent_ids]
    execute_batch(conn.cursor(), """
        INSERT INTO airline_ignores ("Id", agent_id, airline, filter_by_plating_carrier, filter_by_any_segment, filter_by_all_segment, created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(r[0], r[1], trunc(r[2],10) or 'UNK', safe_bool(r[3]), safe_bool(r[4]), safe_bool(r[5]), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("airline_ignores", len(rows))

    # ── 18. CLASS AND NOTES ───────────────────────────────────────────────────
    cur.execute("""
        SELECT "AirlineCode","Class","ShowClass","NonRefundable","Visible",
               "StartAirportCode","EndAirportCode","StartCityCode","EndCityCode",
               "StartCountryCode","EndCountryCode","StartContinentCode","EndContinentCode"
        FROM stg_class_notes
    """)
    rows = cur.fetchall()
    execute_batch(conn.cursor(), """
        INSERT INTO class_and_notes
            (airline_code, class_code, show_class, non_refundable, visible,
             start_airport_code, end_airport_code, start_city_code, end_city_code,
             start_country_code, end_country_code, start_continent_code, end_continent_code,
             created_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, [(trunc(r[0],10) or 'UNK', trunc(r[1],10) or 'Y', trunc(r[2],20),
           safe_bool(r[3]), safe_bool(r[4]),
           trunc(r[5],10), trunc(r[6],10), trunc(r[7],10), trunc(r[8],10),
           trunc(r[9],10), trunc(r[10],10), trunc(r[11],10), trunc(r[12],10), NOW)
          for r in rows], page_size=BATCH)
    conn.commit(); ok("class_and_notes", len(rows))

    # ── 19. BOOKINGS (UUID generation) ────────────────────────────────────────
    print("\n  Generating UUIDs for bookings/flights/segments/passengers...")
    cur.execute("""
        SELECT "Id","AgentId","BookingCode","BookingStatus",
               "DepartureAirportCode","DestinationAirportCode",
               "DepartureDate","ReturnDate","ItineraryType",
               "IBE","GrandTotal","TotalServiceFee","CurrencyCode",
               "ContactName","Email","Phone","PCC",
               "BookingKiwiToken","SelectedValueDeparture",
               "BookingDate","ExpiryDate"
        FROM stg_bookings
    """)
    raw_bookings = cur.fetchall()

    booking_uuid_map = {}  # old_int_id → uuid
    booking_rows = []
    for r in raw_bookings:
        old_id = r[0]
        new_id = uuid.uuid4()
        booking_uuid_map[old_id] = new_id
        agent_code = agent_code_map.get(r[1], 'UNKNOWN')
        session_id = None
        if r[17]: session_id = str(r[17])[:500]
        elif r[18]: session_id = str(r[18])[:500]
        # Use MIGR-{old_id} as fallback booking_code when empty (avoids UNIQUE collision)
        bcode = trunc(r[2], 15) if r[2] and str(r[2]).strip() else f'MIGR-{old_id}'
        booking_rows.append((
            new_id,
            bcode,
            trunc(agent_code, 20) or 'UNKNOWN',
            map_source(r[9]) or 'Datacom',
            map_trip_type(r[8]),
            map_status(r[3]),
            trunc(r[4], 3) or 'SGN',
            trunc(r[5], 3) or 'HAN',
            r[6] or NOW,
            r[7],
            safe_float(r[10]),
            trunc(r[12], 3) or 'VND',
            safe_float(r[11]),
            safe_str(r[13], 100, ''),
            safe_str(r[14], 150, ''),
            safe_str(r[15], 20, ''),
            session_id,
            trunc(r[16], 10),
            None,   # fare_id
            r[20],  # expires_at
            r[19] or NOW,  # created_on_utc
            NOW,           # last_modified_on_utc
        ))

    execute_batch(conn.cursor(), """
        INSERT INTO bookings
            (id, booking_code, agent_code, source, trip_type, status,
             origin, destination, depart_date, return_date,
             total_amount, currency, service_fee,
             contact_name, contact_email, contact_phone,
             session_id, pcc_code, fare_id,
             expires_at, created_on_utc, last_modified_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
        ON CONFLICT DO NOTHING
    """, booking_rows, page_size=BATCH)
    conn.commit(); ok("bookings", len(booking_rows))

    # Get actual inserted booking UUIDs (some may be skipped by ON CONFLICT)
    cur.execute("SELECT id FROM bookings")
    actual_booking_uuids = {r[0] for r in cur.fetchall()}
    print(f"  → Actual bookings in DB: {len(actual_booking_uuids):,}")

    # ── 20. BOOKING FLIGHTS (UUID) ────────────────────────────────────────────
    cur.execute("""
        SELECT "Id","BookingId","StartPoint","EndPoint",
               "StartDate","EndDate","AirrlineCode"
        FROM stg_booking_flights
    """)
    raw_flights = cur.fetchall()
    flight_uuid_map = {}
    flight_rows = []
    for r in raw_flights:
        new_id = uuid.uuid4()
        flight_uuid_map[r[0]] = new_id
        booking_uuid = booking_uuid_map.get(r[1])
        if not booking_uuid or booking_uuid not in actual_booking_uuids: continue
        flight_rows.append((
            new_id, booking_uuid,
            trunc(r[2], 3) or 'SGN',
            trunc(r[3], 3) or 'HAN',
            r[4] or NOW, r[5] or NOW,
            trunc(r[6], 10) or 'VN',
            0,   # stop_count
            NOW, NOW,
        ))
    execute_batch(conn.cursor(), """
        INSERT INTO booking_flights
            (id, booking_id, origin, destination, depart_time, arrive_time,
             airline, stop_count, created_on_utc, last_modified_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, flight_rows, page_size=BATCH)
    conn.commit(); ok("booking_flights", len(flight_rows))

    # Get actual inserted flight UUIDs
    cur.execute("SELECT id FROM booking_flights")
    actual_flight_uuids = {r[0] for r in cur.fetchall()}

    # ── 21. BOOKING SEGMENTS (UUID) ───────────────────────────────────────────
    cur.execute("""
        SELECT "Id","FlightId","FlightNo","AirlineCode",
               "DepartureAirportCode","DestinationAirportCode",
               "DepartureDate","ArrivalDate","Class","AircraftIATA"
        FROM stg_booking_segments
    """)
    raw_segs = cur.fetchall()
    seg_rows = []
    for r in raw_segs:
        flight_uuid = flight_uuid_map.get(r[1])
        if not flight_uuid or flight_uuid not in actual_flight_uuids: continue
        seg_rows.append((
            uuid.uuid4(), flight_uuid,
            trunc(r[2], 10) or 'UNK',
            trunc(r[3], 10) or 'VN',
            trunc(r[4], 3) or 'SGN',
            trunc(r[5], 3) or 'HAN',
            r[6] or NOW, r[7] or NOW,
            trunc(r[8], 5) or 'Y',
            trunc(r[9], 10),
            NOW, NOW,
        ))
    execute_batch(conn.cursor(), """
        INSERT INTO booking_segments
            (id, booking_flight_id, flight_number, airline,
             origin, destination, depart_time, arrive_time,
             cabin_class, aircraft_type,
             created_on_utc, last_modified_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, seg_rows, page_size=BATCH)
    conn.commit(); ok("booking_segments", len(seg_rows))

    # ── 22. PASSENGERS (UUID) ────────────────────────────────────────────────
    cur.execute("""
        SELECT "Id","BookingId","FirstName","LastName","Gender",
               "Birthday","PassengerTypeCode","PassportNumber",
               "PassportExpirationDate","Nationality","Price"
        FROM stg_passengers
    """)
    raw_pax = cur.fetchall()
    pax_uuid_map = {}
    pax_rows = []
    for r in raw_pax:
        new_id = uuid.uuid4()
        pax_uuid_map[r[0]] = new_id
        booking_uuid = booking_uuid_map.get(r[1])
        if not booking_uuid or booking_uuid not in actual_booking_uuids: continue
        # gender: old is bool (True=M, False=F, None=U)
        if r[4] is True:   gender = 'M'
        elif r[4] is False: gender = 'F'
        else:               gender = 'M'  # default
        # birth_date: date field, extract date only
        birth = r[5].date() if hasattr(r[5], 'date') else r[5]
        # passport_expiry: date only
        pp_exp = r[8].date() if hasattr(r[8], 'date') else r[8]
        pax_rows.append((
            new_id, booking_uuid,
            safe_str(r[2], 50, 'Unknown'),
            safe_str(r[3], 50, 'Unknown'),
            None,   # middle_name
            gender,
            birth,
            map_pax_type(r[6]),
            trunc(r[7], 20),
            pp_exp,
            trunc(r[9], 3),
            0.0,    # baggage_kg
            safe_float(r[10]),  # fare_amount
            'VND',  # fare_currency
            NOW, NOW,
        ))
    execute_batch(conn.cursor(), """
        INSERT INTO passengers
            (id, booking_id, first_name, last_name, middle_name, gender,
             birth_date, type, passport_no, passport_expiry, nationality,
             baggage_kg, fare_amount, fare_currency,
             created_on_utc, last_modified_on_utc)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
    """, pax_rows, page_size=BATCH)
    conn.commit(); ok("passengers", len(pax_rows))

    # ── 23. TICKETS ───────────────────────────────────────────────────────────
    if table_exists(cur, "stg_tickets"):
        cur.execute("SELECT booking_code, id FROM bookings")
        code_to_uuid = {r[0]: r[1] for r in cur.fetchall()}
        cur.execute("""
            SELECT "Id","BookingCode","PassengerName","TicketNumber","Airline","IssueDate"
            FROM stg_tickets
        """)
        raw_tickets = cur.fetchall()
        tkt_rows = []
        for r in raw_tickets:
            b_uuid = code_to_uuid.get(r[1])
            if not b_uuid: continue
            tkt_rows.append((
                uuid.uuid4(), b_uuid,
                uuid.uuid4(),  # passenger_id placeholder (no mapping available)
                safe_str(r[3], 30, 'UNKNOWN'),
                safe_str(r[2], 100, 'Unknown'),
                safe_str(r[4], 10, 'UNK'),
                r[5] or NOW,
                NOW, NOW,
            ))
        execute_batch(conn.cursor(), """
            INSERT INTO tickets
                (id, booking_id, passenger_id, ticket_number, passenger_name,
                 airline, issued_at, created_on_utc, last_modified_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, tkt_rows, page_size=BATCH)
        conn.commit(); ok("tickets", len(tkt_rows))
    else:
        skip("tickets")

    # ── 24. INVOICES ──────────────────────────────────────────────────────────
    if table_exists(cur, "stg_invoices"):
        cur.execute("""
            SELECT "Id","BookingId","CompanyName","Address","TaxCode","Receiver"
            FROM stg_invoices
        """)
        rows = cur.fetchall()
        inv_rows = [(booking_uuid_map[r[1]], trunc(r[2],150), trunc(r[3],250),
                     None, trunc(r[4],50), trunc(r[5],100),
                     None, None, 0.0, 'VND', NOW, None, NOW, NOW)
                    for r in rows if r[1] in booking_uuid_map]
        execute_batch(conn.cursor(), """
            INSERT INTO invoices
                (booking_id, company_name, address, city_name, tax_code, receiver,
                 receiver_phone, receiver_email, total_amount, currency,
                 invoice_date, invoice_number, created_on_utc, last_modified_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, inv_rows, page_size=BATCH)
        conn.commit(); ok("invoices", len(inv_rows))
    else:
        skip("invoices")

    # ── 25. TRIP VISAS ────────────────────────────────────────────────────────
    if table_exists(cur, "stg_trip_visas"):
        cur.execute('SELECT "BookingId","Code","Name","Price","Value","Currency","PriceVn" FROM stg_trip_visas')
        rows = cur.fetchall()
        rows_ok = [(booking_uuid_map[r[0]], trunc(r[1],50), trunc(r[2],200),
                    safe_float(r[3]) if r[3] is not None else None,
                    trunc(r[4],500), trunc(r[5],10),
                    safe_float(r[6]) if r[6] is not None else None, NOW)
                   for r in rows if r[0] in booking_uuid_map]
        execute_batch(conn.cursor(), """
            INSERT INTO trip_visas (booking_id, code, name, price, value, currency, price_vn, created_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, rows_ok, page_size=BATCH)
        conn.commit(); ok("trip_visas", len(rows_ok))
    else:
        skip("trip_visas")

    # ── 26. TRIP TOURS ────────────────────────────────────────────────────────
    if table_exists(cur, "stg_trip_tours"):
        cur.execute('SELECT "BookingId","Code","Name","Price","Currency" FROM stg_trip_tours')
        rows = cur.fetchall()
        rows_ok = [(booking_uuid_map[r[0]], trunc(r[2],200), trunc(r[1],50),
                    NOW, NOW, '', 1,
                    safe_float(r[3]), safe_float(r[3]),
                    trunc(r[4],10) or 'VND', 'Active', NOW)
                   for r in rows if r[0] in booking_uuid_map]
        execute_batch(conn.cursor(), """
            INSERT INTO trip_tours
                (booking_id, tour_name, tour_code, departure_date, return_date,
                 destination, pax_count, unit_price, total_amount, currency, status, created_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, rows_ok, page_size=BATCH)
        conn.commit(); ok("trip_tours", len(rows_ok))
    else:
        skip("trip_tours")

    # ── 27. TRIP CANCELLATIONS ────────────────────────────────────────────────
    if table_exists(cur, "stg_trip_cancellations"):
        cur.execute('SELECT "BookingId","MarkupAmount","MarkupPercent","Price","Currency","BookingPrice","Value" FROM stg_trip_cancellations')
        rows = cur.fetchall()
        rows_ok = [(booking_uuid_map[r[0]],
                    safe_float(r[1]), safe_float(r[2]),
                    safe_float(r[3]), trunc(r[4],10) or 'VND',
                    safe_float(r[5]), trunc(r[6],500), NOW)
                   for r in rows if r[0] in booking_uuid_map]
        execute_batch(conn.cursor(), """
            INSERT INTO trip_cancellations
                (booking_id, markup_amount, markup_percent, price, currency,
                 booking_price, value, created_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, rows_ok, page_size=BATCH)
        conn.commit(); ok("trip_cancellations", len(rows_ok))
    else:
        skip("trip_cancellations")

    # ── 28. BAGGAGES (best-effort) ────────────────────────────────────────────
    if table_exists(cur, "stg_baggages"):
        cur.execute('SELECT "BookingId","PassengerId","Baggage","Value","Price" FROM stg_baggages')
        rows = cur.fetchall()
        rows_ok = [(trunc(r[2],50), None, None, None,
                    booking_uuid_map.get(r[0]), NOW)
                   for r in rows if r[0] in booking_uuid_map]
        execute_batch(conn.cursor(), """
            INSERT INTO baggages (baggage_code, pax_id, flight_id, weight, booking_id, created_on_utc)
            VALUES (%s,%s,%s,%s,%s,%s) ON CONFLICT DO NOTHING
        """, rows_ok, page_size=BATCH)
        conn.commit(); ok("baggages", len(rows_ok))
    else:
        skip("baggages")

    # ── Final summary ─────────────────────────────────────────────────────────
    cur.execute("""
        SELECT 'bookings'        , COUNT(*) FROM bookings     UNION ALL
        SELECT 'passengers'      , COUNT(*) FROM passengers   UNION ALL
        SELECT 'booking_flights' , COUNT(*) FROM booking_flights UNION ALL
        SELECT 'booking_segments', COUNT(*) FROM booking_segments UNION ALL
        SELECT 'agents'          , COUNT(*) FROM agents       UNION ALL
        SELECT 'airlines'        , COUNT(*) FROM airlines     UNION ALL
        SELECT 'geo_airports'    , COUNT(*) FROM geo_airports
        ORDER BY 1
    """)
    print("\n  ── Final row counts ───────────────────────────────────────")
    for r in cur.fetchall():
        print(f"  {r[0]:<25} {r[1]:>10,}")

    conn.close()
    print("\n" + "="*60)
    print("  EF Transform complete! Database ready for the application.")
    print("="*60)


if __name__ == "__main__":
    main()
