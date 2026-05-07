-- =============================================================================
-- 03_transform_data.sql
-- Post-pgloader normalization: stg_* → final tables
-- Run AFTER:  01_schema_postgresql.sql (schema)
--         AND 02_pgloader.load         (raw copy)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 0. RENAME pgloader tables (lowercase SQL Server names) → stg_* staging names
-- pgloader lowercases all table names by default, so tblAirlineType → tblairlinetype
-- -----------------------------------------------------------------------------
ALTER TABLE IF EXISTS tblairlinetype      RENAME TO stg_airline_types;
ALTER TABLE IF EXISTS tblairlines         RENAME TO stg_airlines;
ALTER TABLE IF EXISTS tblaircraft         RENAME TO stg_aircrafts;
ALTER TABLE IF EXISTS tblcurrency         RENAME TO stg_currencies;
ALTER TABLE IF EXISTS tblpassengertype    RENAME TO stg_passenger_types;
ALTER TABLE IF EXISTS tblgeocontinent     RENAME TO stg_geo_continents;
ALTER TABLE IF EXISTS tblgeocountry       RENAME TO stg_geo_countries;
ALTER TABLE IF EXISTS tblgeocity          RENAME TO stg_geo_cities;
ALTER TABLE IF EXISTS tblgeoairport       RENAME TO stg_geo_airports;
ALTER TABLE IF EXISTS tblbaggagecontinent RENAME TO stg_baggage_continents;
ALTER TABLE IF EXISTS tblbaggagecountry   RENAME TO stg_baggage_countries;
ALTER TABLE IF EXISTS tblbaggagecity      RENAME TO stg_baggage_cities;
ALTER TABLE IF EXISTS tblbaggageairport   RENAME TO stg_baggage_airports;
ALTER TABLE IF EXISTS tblpcc              RENAME TO stg_pccs;
ALTER TABLE IF EXISTS tblclassandnote     RENAME TO stg_class_notes;
ALTER TABLE IF EXISTS tbluserrole         RENAME TO stg_user_roles;
ALTER TABLE IF EXISTS tbluseraccount      RENAME TO stg_users_website;
ALTER TABLE IF EXISTS tbluseraccountapi   RENAME TO stg_users_api;
ALTER TABLE IF EXISTS tokendevice         RENAME TO stg_token_devices;
ALTER TABLE IF EXISTS tblagent            RENAME TO stg_agents;
ALTER TABLE IF EXISTS tblagentpcc         RENAME TO stg_agent_pccs;
ALTER TABLE IF EXISTS tblpartner          RENAME TO stg_partners;
ALTER TABLE IF EXISTS tblagentpartner     RENAME TO stg_agent_partners;
ALTER TABLE IF EXISTS tbllccinfo          RENAME TO stg_lcc_info;
ALTER TABLE IF EXISTS tblairlineignore    RENAME TO stg_airline_ignores;
ALTER TABLE IF EXISTS tblcommission       RENAME TO stg_commissions;
ALTER TABLE IF EXISTS tblbooking          RENAME TO stg_bookings;
ALTER TABLE IF EXISTS tblbookingflight    RENAME TO stg_booking_flights;
ALTER TABLE IF EXISTS tblbookingsegment   RENAME TO stg_booking_segments;
ALTER TABLE IF EXISTS tblpassenger        RENAME TO stg_passengers;
ALTER TABLE IF EXISTS tblticket           RENAME TO stg_tickets;
ALTER TABLE IF EXISTS tblbaggages         RENAME TO stg_baggages;
ALTER TABLE IF EXISTS tblinsurance        RENAME TO stg_insurances;
ALTER TABLE IF EXISTS tblinvoice          RENAME TO stg_invoices;
ALTER TABLE IF EXISTS tblcarrental        RENAME TO stg_car_rentals;
ALTER TABLE IF EXISTS tbltripcancellation RENAME TO stg_trip_cancellations;
ALTER TABLE IF EXISTS tbltriptour         RENAME TO stg_trip_tours;
ALTER TABLE IF EXISTS tbltripvisa         RENAME TO stg_trip_visas;

BEGIN;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. REFERENCE TABLES
-- ─────────────────────────────────────────────────────────────────────────────

-- airline_types
INSERT INTO airline_types (type, description)
SELECT "Type", "Descr"
FROM   stg_airline_types
ON CONFLICT (type) DO NOTHING;

-- airlines
INSERT INTO airlines (code, name_vi, name_en, name_fr, logo, description, airline_type, visible)
SELECT "Code", "Name_Vi", "Name_En", "Name_Fr", "Logo", "Description",
       NULLIF("AirlineType",''), "Visible"::boolean
FROM   stg_airlines
ON CONFLICT (code) DO NOTHING;

-- aircrafts
INSERT INTO aircrafts (iata, manufacturer, model, visible)
SELECT "IATA", "Manufacturer", "Model", "Visible"::boolean
FROM   stg_aircrafts
ON CONFLICT (iata) DO NOTHING;

-- currencies  (RoundUnit → round_unit; Lock → locked)
INSERT INTO currencies (code, name, rate, symbol, round_unit, locked, active)
SELECT "Code", "Name", "Rate"::numeric(18,6), "Symbol",
       "RoundUnit"::numeric(18,6), "Lock"::boolean, "Active"::boolean
FROM   stg_currencies
ON CONFLICT (code) DO NOTHING;

-- passenger_types
INSERT INTO passenger_types (code, icon, name_vi, name_en, name_fr, description)
SELECT "Code", "Icon", "Name_Vi", "Name_En", "Name_Fr", "Description"
FROM   stg_passenger_types
ON CONFLICT (code) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. GEO TABLES  (merge tblGeoXxx + tblBaggageXxx)
-- ─────────────────────────────────────────────────────────────────────────────

-- geo_continents: tblGeoContinent + tblBaggageContinent (dedup by code)
INSERT INTO geo_continents (code, name_vi, name_en, name_fr, visible)
SELECT "Code", "Name_Vi", "Name_En", "Name_Fr", "Visible"::boolean
FROM   stg_geo_continents
UNION
SELECT "Code", "Name_Vi", "Name_En", "Name_Fr", "Visible"::boolean
FROM   stg_baggage_continents
ON CONFLICT (code) DO NOTHING;

-- geo_countries
INSERT INTO geo_countries (code, continent_code, name_vi, name_en, name_fr, flag, visible)
SELECT "Code", "ContinentCode", "Name_Vi", "Name_En", "Name_Fr", "Flag", "Visible"::boolean
FROM   stg_geo_countries
UNION
SELECT "Code", "ContinentCode", "Name_Vi", "Name_En", "Name_Fr", "Flag", "Visible"::boolean
FROM   stg_baggage_countries
ON CONFLICT (code) DO NOTHING;

-- geo_cities  (SearchKeys comes only from tblGeoCity)
INSERT INTO geo_cities (code, country_code, name_vi, name_en, name_fr, location, search_keys, visible)
SELECT "Code", "CountryCode", "Name_Vi", "Name_En", "Name_Fr", "Location", "SearchKeys", "Visible"::boolean
FROM   stg_geo_cities
ON CONFLICT (code) DO NOTHING;

-- Also insert baggage cities that don't exist in geo_cities
INSERT INTO geo_cities (code, country_code, name_vi, name_en, name_fr, location, visible)
SELECT "Code", "CountryCode", "Name_Vi", "Name_En", "Name_Fr", "Location", "Visible"::boolean
FROM   stg_baggage_cities
WHERE  "Code" NOT IN (SELECT code FROM geo_cities)
ON CONFLICT (code) DO NOTHING;

-- geo_airports
INSERT INTO geo_airports (code, city_code, name_vi, name_en, name_fr, location, search_keys, visible)
SELECT "Code", "CityCode", "Name_Vi", "Name_En", "Name_Fr", "Location", "SearchKeys", "Visible"::boolean
FROM   stg_geo_airports
ON CONFLICT (code) DO NOTHING;

INSERT INTO geo_airports (code, city_code, name_vi, name_en, name_fr, location, visible)
SELECT "Code", "CityCode", "Name_Vi", "Name_En", "Name_Fr", "Location", "Visible"::boolean
FROM   stg_baggage_airports
WHERE  "Code" NOT IN (SELECT code FROM geo_airports)
ON CONFLICT (code) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. GALILEO PCC
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO pccs (pcc, active)
SELECT "Pcc", "Active"::boolean
FROM   stg_pccs
ON CONFLICT (pcc) DO NOTHING;

-- class_notes  (tblClassAndNote → class_notes; PascalCase → snake_case)
INSERT INTO class_notes (
    airline_code, class, show_class, non_refundable, visible,
    start_airport_code, end_airport_code, start_city_code, end_city_code,
    start_country_code, end_country_code, start_continent_code, end_continent_code)
SELECT
    "AirlineCode", "Class", "ShowClass", "NonRefundable"::boolean, "Visible"::boolean,
    "StartAirportCode", "EndAirportCode", "StartCityCode", "EndCityCode",
    "StartCountryCode", "EndCountryCode", "StartContinentCode", "EndContinentCode"
FROM stg_class_notes;

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. USERS  (merge tblUserAccount + tblUserAccountAPI into users)
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO user_roles (id, name, description)
SELECT "Id", "Name", "Description"
FROM   stg_user_roles
ON CONFLICT (id) DO NOTHING;

-- Preserve original IDs so foreign keys from tblBooking etc. still resolve.
-- tblUserAccount → source='website', tblUserAccountAPI → source='api'
-- Gender: old bit (1=male, 0=female, NULL=unknown) → new char(1) M/F/U
INSERT INTO users (
    id, user_role_id, source, email, password_hash, phone, full_name,
    gender, address, avatar, otp, ip_last_login, active, visible,
    created_at, last_login_at)
SELECT
    "Id",
    "UserRoleId",
    'website',
    "Email",
    "Password",   -- was stored as plain text / MD5 in old system
    "Phone",
    "FullName",
    CASE WHEN "Gender" IS NULL THEN 'U'
         WHEN "Gender" = '1'  THEN 'M'
         ELSE 'F' END,
    "Address",
    "Avatar",
    "OTP",
    "IPLastLogin",
    COALESCE("Active"::boolean, FALSE),
    COALESCE("Visible"::boolean, TRUE),
    COALESCE("CreateDate"::timestamptz, now()),
    "LastLoginDate"::timestamptz
FROM stg_users_website
ON CONFLICT (id) DO NOTHING;

-- Reset sequence so API user auto-IDs don't collide with website user IDs
SELECT setval('users_id_seq', COALESCE((SELECT MAX(id) FROM users), 0));

-- API users (auto-generated IDs, no explicit id column)
INSERT INTO users (
    user_role_id, source, email, password_hash, phone, full_name,
    gender, address, avatar, otp, ip_last_login, active, visible,
    created_at, last_login_at)
SELECT
    "UserRoleId",
    'api',
    "Email",
    "Password",
    "Phone",
    "FullName",
    CASE WHEN "Gender" IS NULL THEN 'U'
         WHEN "Gender" = '1'  THEN 'M'
         ELSE 'F' END,
    "Address",
    "Avatar",
    "OTP",
    "IPLastLogin",
    COALESCE("Active"::boolean, FALSE),
    COALESCE("Visible"::boolean, TRUE),
    COALESCE("CreateDate"::timestamptz, now()),
    "LastLoginDate"::timestamptz
FROM stg_users_api;

-- token_devices (only from tblTokenDevice — no user FK in old table)
INSERT INTO token_devices (id, token, device_os, device_name)
SELECT "Id", "Token", "DeviceOs", "DeviceName"
FROM   stg_token_devices
ON CONFLICT (id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. AGENTS
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO agents (
    id, agent_code, name, address, tel, email, password_hash,
    galileo_pcc, galileo_hcm, galileo_connect_type,
    galileo_book_pcc, galileo_book_hcm, galileo_book_connect_type, galileo_active,
    lcc_vn_active_domestic, lcc_vn_active_global, lcc_all_class, lcc_ignore_promo,
    enable_cache, cache_time_minutes, send_mail_in_api, email_sender_id,
    combined_mode, baggage_fee_percent, baggage_fee_amount,
    active, visible, created_at, expiry_date)
SELECT
    "Id", "AgentCode", "Name", "Address", "Tel", "Email", "Password",
    "GalileoPcc", "GalileoHcm", "GalileoConnectType"::boolean,
    "GalileoBookPcc", "GalileoBookHcm", "GalileoBookConnectType"::boolean, "GalileoActive"::boolean,
    "Lcc_VN_Active_Domestic"::boolean, "Lcc_VN_Active_Global"::boolean, "Lcc_All_Class"::boolean, "Lcc_Ignore_Promo"::boolean,
    "EnableCache"::boolean, "CacheTime"::integer, "SendMailInApi"::boolean, NULLIF("EmailSender"::integer, 0),
    "CombinedMode"::integer,
    "BaggageFeePercent"::numeric(10,4),
    "BaggageFeeAmount"::numeric(18,4),
    "Active"::boolean, "Visible"::boolean,
    COALESCE("CreateDate"::timestamptz, now()),
    "ExpiryDate"::timestamptz
FROM stg_agents
ON CONFLICT DO NOTHING;

INSERT INTO agent_pccs (id, agent_id, pcc, ignored_mode, list_start_point, active)
SELECT "Id"::integer, "AgentId"::integer, "Pcc", "IgnoredMode"::integer, "ListStartPoint", "Active"::boolean
FROM   stg_agent_pccs
WHERE  "AgentId"::integer IN (SELECT id FROM agents)
ON CONFLICT DO NOTHING;

INSERT INTO partners (id, name, active)
SELECT "Id"::integer, "Name", "Active"::boolean
FROM   stg_partners
ON CONFLICT (id) DO NOTHING;

INSERT INTO agent_partners (id, agent_id, partner_id, ignored_mode, list_start_point, active)
SELECT "Id"::integer, "AgentId"::integer, "PartnerId"::integer, "IgnoredMode"::integer, "ListStartPoint", "Active"::boolean
FROM   stg_agent_partners
WHERE  "AgentId"::integer IN (SELECT id FROM agents)
ON CONFLICT DO NOTHING;

INSERT INTO agent_lcc_info (agent_id, airline_code, allow_search, allow_book)
SELECT "AgentId"::integer, "Airline", "AllowSearch"::boolean, "AllowBook"::boolean
FROM   stg_lcc_info
WHERE  "AgentId"::integer IN (SELECT id FROM agents)
ON CONFLICT DO NOTHING;

INSERT INTO agent_airline_ignores (id, agent_id, airline_code, filter_by_plating, filter_by_any_segment, filter_by_all_segments)
SELECT "Id", "AgentId", "Airline",
       "FilterByPlatingCarrier"::boolean, "FilterByAnySegment"::boolean, "FilterByAllSegment"::boolean
FROM   stg_airline_ignores
WHERE  "AgentId"::integer IN (SELECT id FROM agents)
ON CONFLICT DO NOTHING;

-- commissions  (Float → numeric casts handled by pgloader already)
INSERT INTO commissions (
    id, agent_id, airline_group, start_region, end_region, currency,
    fee_adt_one_way, fee_chd_one_way, fee_inf_one_way,
    fee_adt_round_trip, fee_chd_round_trip, fee_inf_round_trip, fee_by_percent,
    commission, com_by_percent, com_percent_on_base_fare)
SELECT
    "Id", "AgentId", "AirlineGroup", "StartRegion", "EndRegion", "Currency",
    "FeeAdtOneWay"::numeric(18,4), "FeeChdOneWay"::numeric(18,4), "FeeInfOneWay"::numeric(18,4),
    "FeeAdtRoundTrip"::numeric(18,4), "FeeChdRoundTrip"::numeric(18,4), "FeeInfRoundTrip"::numeric(18,4),
    "FeeByPercent"::numeric(10,4),
    "Commission"::numeric(18,4), "ComByPercent"::boolean, "ComPercentOnBasicFare"::boolean
FROM stg_commissions
WHERE "AgentId"::integer IN (SELECT id FROM agents)
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. BOOKINGS
-- (tblBooking → bookings + booking_contacts + booking_hotels)
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO bookings (
    id, agent_id, booking_code, booking_status,
    departure_airport, destination_airport, departure_date, return_date,
    itinerary_type, airline_code, return_airline_code,
    ibe_source, pcc,
    -- engine_session: consolidate legacy kiwi/selected-value columns
    engine_session,
    total_price, total_service_fee, grand_total, currency_code,
    grand_total_usd, vnd_usd_rate, grand_total_origin, origin_currency,
    origin_currency_usd_rate,
    total_baggage_price, total_insurance_price, total_travel_sim_price,
    total_travel_insurance_price, total_car_rental_price, total_trip_cancel_price,
    total_trip_tour_price, total_trip_visa_price, total_trip_hotel_price,
    passenger_count, fare_data_id, departure_flight_id, return_flight_id,
    difference, pay_with_agency_credit, has_invoice, search_params,
    lang, url_refer, remark, ip_address, visible,
    booked_at, expiry_at, created_at, updated_at)
SELECT
    "Id",
    CASE WHEN "AgentId"::integer IN (SELECT id FROM agents) THEN "AgentId"::integer ELSE NULL END,
    "BookingCode",
    COALESCE("BookingStatus", 'PENDING'),
    "DepartureAirportCode",
    "DestinationAirportCode",
    "DepartureDate"::date,
    "ReturnDate"::date,
    "ItineraryType",
    "AirlineCode",
    "ReturnAirlineCode",
    "IBE",
    "PCC",
    -- Merge engine-specific token columns into engine_session JSON
    CASE
      WHEN "BookingKiwiToken" IS NOT NULL AND "BookingKiwiToken" <> ''
      THEN json_build_object(
               'kiwiToken', "BookingKiwiToken",
               'kiwiId',    "BookingKiwiId",
               'selectedDep', "SelectedValueDeparture",
               'selectedRet', "SelectedValueReturn")::text
      WHEN "SelectedValueDeparture" IS NOT NULL AND "SelectedValueDeparture" <> ''
      THEN json_build_object(
               'selectedDep', "SelectedValueDeparture",
               'selectedRet', "SelectedValueReturn")::text
      ELSE NULL
    END,
    "TotalPrice"::numeric(18,4),
    "TotalServiceFee"::numeric(18,4),
    "GrandTotal"::numeric(18,4),
    "CurrencyCode",
    "GrandTotalUsd"::numeric(18,4),
    "VndUsdRate"::numeric(18,6),
    "GrandTotalOrigin"::numeric(18,4),
    "OriginCurrency",
    "OriginCurrencyUsdRate"::numeric(18,6),
    "TotalBaggagePrice"::numeric(18,4),
    "TotalInsurancePrice"::numeric(18,4),
    "TotalTravelSimPrice"::numeric(18,4),
    "TotalTravelInsurancePrice"::numeric(18,4),
    "TotalCarRentalPrice"::numeric(18,4),
    "TotalTripCancelPrice"::numeric(18,4),
    "TotalTripTourPrice"::numeric(18,4),
    "TotalTripVisaPrice"::numeric(18,4),
    "TotalTripHotelPrice"::numeric(18,4),
    "PassengerNo",
    NULLIF("FareDataId"::integer, 0),
    NULLIF("DepartureFlightId"::integer, 0),
    NULLIF("ReturnFlightId"::integer, 0),
    "Difference"::numeric(18,4),
    "PayWithAgencyCredit"::boolean,
    "HasInvoice"::boolean,
    "SearchParams",
    "Lang",
    "UrlRefer",
    "Remark",
    "IPAddress",
    "Visible"::boolean,
    COALESCE("BookingDate"::timestamptz, now()),
    "ExpiryDate"::timestamptz,
    COALESCE("BookingDate"::timestamptz, now()),
    COALESCE("BookingDate"::timestamptz, now())
FROM stg_bookings
ON CONFLICT (id) DO NOTHING;

-- booking_contacts: extracted from stg_bookings
INSERT INTO booking_contacts (booking_id, name, title, email, phone, address, city, zip_code, country_name)
SELECT
    "Id",
    "ContactName",
    CASE WHEN "ContactTitle" = '1' THEN 'M' ELSE 'F' END,
    "Email",
    "Phone",
    "Address",
    "City",
    "ZipCode",
    "CountryName"
FROM stg_bookings
WHERE "ContactName" IS NOT NULL
   OR "Email" IS NOT NULL
   OR "Phone" IS NOT NULL
ON CONFLICT (booking_id) DO NOTHING;

-- booking_hotels: extracted from stg_bookings
INSERT INTO booking_hotels (booking_id, hotel_no, hotel_name, hotel_room)
SELECT
    "Id",
    "BookingHotelNo",
    "BookingHotelName",
    "BookingHotelRoom"
FROM stg_bookings
WHERE "BookingHotelNo" IS NOT NULL
   OR "BookingHotelName" IS NOT NULL
ON CONFLICT (booking_id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. BOOKING FLIGHTS & SEGMENTS
-- ─────────────────────────────────────────────────────────────────────────────

-- Check schema for booking_flights in 01_schema_postgresql.sql
-- NOTE: column AirrlineCode (typo) → airline_code
INSERT INTO booking_flights (id, booking_id, itinerary, start_point, end_point,
    start_date, end_date, airline_code, total_flight_time, flight_number)
SELECT
    "Id", "BookingId", "Itinerary", "StartPoint", "EndPoint",
    "StartDate", "EndDate",
    "AirrlineCode",  -- old typo column name
    "TotalFlightTime"::numeric(10,2),
    "FlightNumber"
FROM stg_booking_flights
ON CONFLICT (id) DO NOTHING;

INSERT INTO booking_segments (
    id, flight_id, departure_date, arrival_date, class,
    start_terminal, end_terminal,
    departure_city_code, destination_city_code,
    departure_airport_code, destination_airport_code,
    airline_code, op_airline_code, flight_no, flight_duration,
    aircraft_iata, items_no, stop_point, stop_time,
    is_last_item, different_day, overnight_stop,
    change_terminal, change_airport, transit,
    airport_change_to)
SELECT
    "Id", "FlightId", "DepartureDate", "ArrivalDate", LEFT("Class", 50),
    LEFT(TRIM("StartTerminal"), 10), LEFT(TRIM("EndTerminal"), 10),
    LEFT("DepartureCityCode", 10), LEFT("DestinationCityCode", 10),
    LEFT("DepartureAirportCode", 10), LEFT("DestinationAirportCode", 10),
    LEFT("AirlineCode", 10), LEFT("OpAirlineCode", 10), LEFT("FlightNo", 50),
    "FlightDuration"::numeric(10,4),
    LEFT("AircraftIATA", 10), "ItemsNo", LEFT("StopPoint", 10),
    "StopTime"::numeric(10,4),
    "IsLastItem"::boolean, "DifferentDay"::boolean, "OvernightStop"::boolean,
    "ChangeTerminal"::boolean, "ChangeAirport"::boolean, "Transit"::boolean,
    LEFT("AirportChangeTo", 10)
FROM stg_booking_segments
ON CONFLICT DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 8. PASSENGERS
-- ─────────────────────────────────────────────────────────────────────────────

INSERT INTO passengers (
    id, booking_id, passenger_type, first_name, last_name,
    gender, birthday, passport_number, passport_expiry_date,
    fare, tax, fee, service_fee, price, nationality)
SELECT
    "Id", "BookingId", "PassengerTypeCode",
    "FirstName", "LastName",
    CASE WHEN "Gender" IS NULL THEN 'U'
         WHEN "Gender" = '1'  THEN 'M'
         ELSE 'F' END,
    "Birthday"::date,
    "PassportNumber",
    "PassportExpirationDate"::date,
    "Fare"::numeric(18,4),
    "Tax"::numeric(18,4),
    "Fee"::numeric(18,4),
    "ServiceFee"::numeric(18,4),
    "Price"::numeric(18,4),
    "Nationality"
FROM stg_passengers
ON CONFLICT (id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- 9. TICKETS
-- ─────────────────────────────────────────────────────────────────────────────

DO $$
BEGIN
  IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'stg_tickets') THEN
    INSERT INTO tickets (id, airline_code, booking_code, passenger_name, ticket_number, response, issued_at)
    SELECT "Id", "Airline", "BookingCode", "PassengerName", "TicketNumber", "Response", "IssueDate"
    FROM   stg_tickets
    ON CONFLICT DO NOTHING;
  END IF;
END $$;

-- ─────────────────────────────────────────────────────────────────────────────
-- 10. RESET ALL SEQUENCES to max(id) + 1
-- ─────────────────────────────────────────────────────────────────────────────

SELECT setval('users_id_seq',              COALESCE((SELECT MAX(id) FROM users), 1));
SELECT setval('agents_id_seq',             COALESCE((SELECT MAX(id) FROM agents), 1));
SELECT setval('bookings_id_seq',           COALESCE((SELECT MAX(id) FROM bookings), 1));
SELECT setval('booking_contacts_id_seq',   COALESCE((SELECT MAX(id) FROM booking_contacts), 1));
SELECT setval('booking_hotels_id_seq',     COALESCE((SELECT MAX(id) FROM booking_hotels), 1));
SELECT setval('booking_flights_id_seq',    COALESCE((SELECT MAX(id) FROM booking_flights), 1));
SELECT setval('booking_segments_id_seq',   COALESCE((SELECT MAX(id) FROM booking_segments), 1));
SELECT setval('passengers_id_seq',         COALESCE((SELECT MAX(id) FROM passengers), 1));
SELECT setval('tickets_id_seq',            COALESCE((SELECT MAX(id) FROM tickets), 1));
SELECT setval('commissions_id_seq',        COALESCE((SELECT MAX(id) FROM commissions), 1));
SELECT setval('class_notes_id_seq',        COALESCE((SELECT MAX(id) FROM class_notes), 1));
SELECT setval('user_roles_id_seq',         COALESCE((SELECT MAX(id) FROM user_roles), 1));
SELECT setval('agent_pccs_id_seq',         COALESCE((SELECT MAX(id) FROM agent_pccs), 1));
SELECT setval('partners_id_seq',           COALESCE((SELECT MAX(id) FROM partners), 1));
SELECT setval('agent_partners_id_seq',     COALESCE((SELECT MAX(id) FROM agent_partners), 1));
SELECT setval('agent_airline_ignores_id_seq', COALESCE((SELECT MAX(id) FROM agent_airline_ignores), 1));
SELECT setval('token_devices_id_seq',      COALESCE((SELECT MAX(id) FROM token_devices), 1));

-- ─────────────────────────────────────────────────────────────────────────────
-- 11. DROP staging tables (clean-up)
-- ─────────────────────────────────────────────────────────────────────────────

DROP TABLE IF EXISTS
    stg_airline_types, stg_airlines, stg_aircrafts, stg_currencies,
    stg_passenger_types,
    stg_geo_continents, stg_geo_countries, stg_geo_cities, stg_geo_airports,
    stg_baggage_continents, stg_baggage_countries, stg_baggage_cities, stg_baggage_airports,
    stg_pccs, stg_class_notes,
    stg_user_roles, stg_users_website, stg_users_api, stg_token_devices,
    stg_agents, stg_agent_pccs, stg_partners, stg_agent_partners,
    stg_lcc_info, stg_airline_ignores, stg_commissions,
    stg_bookings, stg_booking_flights, stg_booking_segments,
    stg_passengers, stg_tickets,
    stg_baggages, stg_insurances, stg_invoices,
    stg_car_rentals, stg_trip_cancellations, stg_trip_tours, stg_trip_visas
CASCADE;

COMMIT;

-- Done. Verify row counts:
SELECT 'airline_types'    AS tbl, COUNT(*) FROM airline_types     UNION ALL
SELECT 'airlines',                 COUNT(*) FROM airlines          UNION ALL
SELECT 'geo_airports',             COUNT(*) FROM geo_airports      UNION ALL
SELECT 'agents',                   COUNT(*) FROM agents            UNION ALL
SELECT 'commissions',              COUNT(*) FROM commissions       UNION ALL
SELECT 'bookings',                 COUNT(*) FROM bookings          UNION ALL
SELECT 'passengers',               COUNT(*) FROM passengers        UNION ALL
SELECT 'tickets',                  COUNT(*) FROM tickets
ORDER BY 1;
