"""
Migration script: SQL Server (SearchAPI) → PostgreSQL (flight_api)
Copies 34 tables into stg_* staging tables with proper column types.
Run AFTER 01_schema_postgresql.sql
"""

import pyodbc
import psycopg2
from psycopg2.extras import execute_batch
from decimal import Decimal
from datetime import datetime, date
import sys

# ── Connection config ──────────────────────────────────────────────────────────
MSSQL = {
    "driver": "ODBC Driver 17 for SQL Server",
    "server": "127.0.0.1,14330",
    "database": "SearchAPI",
    "user": "sa",
    "password": "U76hqeHk1LMgbU2",
}

PG = {
    "host": "localhost",
    "port": 5432,
    "dbname": "flight_api",
    "user": "postgres",
    "password": "123456",
}

# ── Tables: SQL Server → PostgreSQL staging name ───────────────────────────────
TABLES = [
    ("tblAirlineType",      "stg_airline_types"),
    ("tblAirlines",         "stg_airlines"),
    ("tblAircraft",         "stg_aircrafts"),
    ("tblCurrency",         "stg_currencies"),
    ("tblPassengerType",    "stg_passenger_types"),
    ("tblGeoContinent",     "stg_geo_continents"),
    ("tblGeoCountry",       "stg_geo_countries"),
    ("tblGeoCity",          "stg_geo_cities"),
    ("tblGeoAirport",       "stg_geo_airports"),
    ("tblBaggageContinent", "stg_baggage_continents"),
    ("tblBaggageCountry",   "stg_baggage_countries"),
    ("tblBaggageCity",      "stg_baggage_cities"),
    ("tblBaggageAirport",   "stg_baggage_airports"),
    ("tblPcc",              "stg_pccs"),
    ("tblClassAndNote",     "stg_class_notes"),
    ("tblUserRole",         "stg_user_roles"),
    ("tblUserAccount",      "stg_users_website"),
    ("tblUserAccountAPI",   "stg_users_api"),
    ("TokenDevice",         "stg_token_devices"),
    ("tblAgent",            "stg_agents"),
    ("tblAgentPcc",         "stg_agent_pccs"),
    ("tblPartner",          "stg_partners"),
    ("tblAgentPartner",     "stg_agent_partners"),
    ("tblLccInfo",          "stg_lcc_info"),
    ("tblAirlineIgnore",    "stg_airline_ignores"),
    ("tblCommission",       "stg_commissions"),
    ("tblBooking",          "stg_bookings"),
    ("tblBookingFlight",    "stg_booking_flights"),
    ("tblBookingSegment",   "stg_booking_segments"),
    ("tblPassenger",        "stg_passengers"),
    ("tblTicket",           "stg_tickets"),
    ("tblBaggages",         "stg_baggages"),
    ("tblInsurance",        "stg_insurances"),
    ("tblInvoice",          "stg_invoices"),
    ("tblCarRental",        "stg_car_rentals"),
    ("tblTripCancellation", "stg_trip_cancellations"),
    ("tblTripTour",         "stg_trip_tours"),
    ("tblTripVisa",         "stg_trip_visas"),
]

BATCH_SIZE = 500


def py_type_to_pg(val):
    """Map Python value type → PostgreSQL column type."""
    if isinstance(val, bool):
        return "BOOLEAN"
    if isinstance(val, int):
        return "BIGINT"
    if isinstance(val, float):
        return "DOUBLE PRECISION"
    if isinstance(val, Decimal):
        return "NUMERIC"
    if isinstance(val, datetime):
        return "TIMESTAMPTZ"
    if isinstance(val, date):
        return "DATE"
    return "TEXT"


def detect_col_types(mssql_cur, src_table, columns):
    """Sample up to 200 rows to infer PostgreSQL column types."""
    types = ["TEXT"] * len(columns)
    mssql_cur.execute(f"SELECT TOP 200 * FROM [{src_table}]")
    for row in mssql_cur.fetchall():
        for i, val in enumerate(row):
            if val is None:
                continue
            pg_type = py_type_to_pg(val)
            # Upgrade TEXT → specific type; keep more specific if already set
            if types[i] == "TEXT" and pg_type != "TEXT":
                types[i] = pg_type
    return types


def clean_val(val):
    """Pass through native types; psycopg2 handles them correctly."""
    return val  # None, bool, int, float, Decimal, datetime, str all work natively


def connect_mssql():
    conn_str = (
        f"DRIVER={{{MSSQL['driver']}}};"
        f"SERVER={MSSQL['server']};"
        f"DATABASE={MSSQL['database']};"
        f"UID={MSSQL['user']};"
        f"PWD={MSSQL['password']};"
        f"TrustServerCertificate=yes;"
    )
    return pyodbc.connect(conn_str)


def connect_pg():
    return psycopg2.connect(**PG)


def migrate_table(mssql_conn, pg_conn, src_table, dst_table):
    mssql_cur = mssql_conn.cursor()

    # Detect types from sample
    mssql_cur.execute(f"SELECT TOP 1 * FROM [{src_table}]")
    columns = [col[0] for col in mssql_cur.description]
    col_types = detect_col_types(mssql_cur, src_table, columns)

    # Fetch all rows
    mssql_cur.execute(f"SELECT * FROM [{src_table}]")
    rows = mssql_cur.fetchall()
    mssql_cur.close()

    if not rows:
        print(f"  [SKIP] {src_table} — 0 rows")
        return 0

    # Build quoted column list
    quoted_cols = ", ".join(f'"{c}"' for c in columns)
    placeholders = ", ".join(["%s"] * len(columns))

    pg_cur = pg_conn.cursor()

    # Drop + create staging table with proper types
    col_defs = ", ".join(f'"{c}" {t}' for c, t in zip(columns, col_types))
    pg_cur.execute(f'DROP TABLE IF EXISTS "{dst_table}" CASCADE')
    pg_cur.execute(f'CREATE TABLE "{dst_table}" ({col_defs})')
    pg_conn.commit()

    # Insert in batches (values passed as native Python types)
    converted = [tuple(clean_val(v) for v in row) for row in rows]
    insert_sql = f'INSERT INTO "{dst_table}" ({quoted_cols}) VALUES ({placeholders})'
    execute_batch(pg_cur, insert_sql, converted, page_size=BATCH_SIZE)
    pg_conn.commit()
    pg_cur.close()

    return len(rows)


def main():
    print("Connecting to SQL Server...")
    try:
        mssql_conn = connect_mssql()
    except Exception as e:
        print(f"ERROR: Cannot connect to SQL Server: {e}")
        print("Make sure PuTTY tunnel is active (127.0.0.1:14330)")
        sys.exit(1)

    print("Connecting to PostgreSQL...")
    try:
        pg_conn = connect_pg()
    except Exception as e:
        print(f"ERROR: Cannot connect to PostgreSQL: {e}")
        sys.exit(1)

    total_tables = len(TABLES)
    total_rows = 0

    print(f"\nMigrating {total_tables} tables...\n")
    for i, (src, dst) in enumerate(TABLES, 1):
        try:
            count = migrate_table(mssql_conn, pg_conn, src, dst)
            total_rows += count
            print(f"  [{i:2d}/{total_tables}] {src:30s} → {dst:35s}  {count:>8,} rows")
        except Exception as e:
            print(f"  [{i:2d}/{total_tables}] {src:30s} → ERROR: {e}")
            pg_conn.rollback()

    mssql_conn.close()
    pg_conn.close()

    print(f"\nDone! Total rows migrated: {total_rows:,}")
    print("Next step: run 03_transform_data.sql")


if __name__ == "__main__":
    main()
