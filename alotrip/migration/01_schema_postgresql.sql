-- =============================================================================
-- PostgreSQL Schema mới — đã cải tiến từ SQL Server schema cũ
-- Database: flightapi
-- Nguyên tắc:
--   1. Float → numeric(18,4) cho tiền tệ
--   2. Geo tables hợp nhất (bỏ tblBaggageXxx trùng lặp)
--   3. tblBooking tách contact + engine_data + add-on totals ra riêng
--   4. tblUserAccount + tblUserAccountAPI gộp thành users
--   5. Bỏ tblCache (dùng Redis) + tblConfigSystemLog (dùng Loki)
--   6. Thêm audit columns (created_at, updated_at) cho các bảng quan trọng
--   7. Gender: bit → char(1) CHECK ('M','F','U')
--   8. NText → text
--   9. Sửa typo AirrlineCode → airline_code
--  10. snake_case nhất quán
-- =============================================================================

-- Đảm bảo extension uuid
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- 1. LOOKUP / REFERENCE TABLES
-- =============================================================================

-- Airline type (DOM, INT, LCC...)
CREATE TABLE airline_types (
    type        varchar(10) PRIMARY KEY,
    description varchar(500)
);

-- Airlines
CREATE TABLE airlines (
    code         varchar(10)  PRIMARY KEY,
    name_vi      varchar(150),
    name_en      varchar(150),
    name_fr      varchar(150),
    logo         varchar(50),
    description  varchar(50),
    airline_type varchar(10)  REFERENCES airline_types(type),
    visible      boolean      NOT NULL DEFAULT true
);

-- Aircraft
CREATE TABLE aircrafts (
    iata         varchar(10)  PRIMARY KEY,
    manufacturer varchar(150),
    model        varchar(150),
    visible      boolean NOT NULL DEFAULT true
);

-- Currencies
CREATE TABLE currencies (
    code       varchar(10)   PRIMARY KEY,
    name       varchar(150),
    rate       numeric(18,6) NOT NULL DEFAULT 1,
    symbol     varchar(10),
    round_unit numeric(18,6) NOT NULL DEFAULT 1000, -- VND=1000, USD=0.01
    locked     boolean       NOT NULL DEFAULT false,
    active     boolean       NOT NULL DEFAULT true
);

-- Passenger types (ADT, CHD, INF)
CREATE TABLE passenger_types (
    code        varchar(10)  PRIMARY KEY,
    icon        varchar(50),
    name_vi     varchar(150),
    name_en     varchar(150),
    name_fr     varchar(150),
    description varchar(150)
);

-- =============================================================================
-- 2. GEOGRAPHY (hợp nhất tblGeoXxx + tblBaggageXxx)
-- =============================================================================

CREATE TABLE geo_continents (
    code    varchar(10)  PRIMARY KEY,
    name_vi varchar(150),
    name_en varchar(150),
    name_fr varchar(150),
    visible boolean NOT NULL DEFAULT true
);

CREATE TABLE geo_countries (
    code           varchar(10)  PRIMARY KEY,
    continent_code varchar(10)  NOT NULL REFERENCES geo_continents(code),
    name_vi        varchar(150),
    name_en        varchar(150),
    name_fr        varchar(150),
    flag           varchar(150),
    visible        boolean NOT NULL DEFAULT true
);
CREATE INDEX idx_geo_countries_continent ON geo_countries(continent_code);

CREATE TABLE geo_cities (
    code         varchar(10)  PRIMARY KEY,
    country_code varchar(10)  NOT NULL REFERENCES geo_countries(code),
    name_vi      varchar(150),
    name_en      varchar(150),
    name_fr      varchar(150),
    location     varchar(50),
    search_keys  text,
    visible      boolean NOT NULL DEFAULT true
);
CREATE INDEX idx_geo_cities_country ON geo_cities(country_code);

CREATE TABLE geo_airports (
    code        varchar(10)  PRIMARY KEY,
    city_code   varchar(10)  NOT NULL REFERENCES geo_cities(code),
    name_vi     varchar(150),
    name_en     varchar(150),
    name_fr     varchar(150),
    location    varchar(50),
    search_keys text,
    visible     boolean NOT NULL DEFAULT true
);
CREATE INDEX idx_geo_airports_city ON geo_airports(city_code);

-- =============================================================================
-- 3. GALILEO PCC
-- =============================================================================

CREATE TABLE pccs (
    pcc    varchar(10) PRIMARY KEY,
    active boolean NOT NULL DEFAULT true
);

-- Cabin class / fare notes per airline
CREATE TABLE class_notes (
    id                    serial      PRIMARY KEY,
    airline_code          varchar(10) NOT NULL,
    class                 text,
    show_class            text,
    non_refundable        boolean     NOT NULL DEFAULT false,
    visible               boolean     NOT NULL DEFAULT true,
    start_airport_code    varchar(10),
    end_airport_code      varchar(10),
    start_city_code       varchar(10),
    end_city_code         varchar(10),
    start_country_code    varchar(10),
    end_country_code      varchar(10),
    start_continent_code  varchar(10),
    end_continent_code    varchar(10)
);

-- =============================================================================
-- 4. USERS (gộp tblUserAccount + tblUserAccountAPI)
-- =============================================================================

CREATE TABLE user_roles (
    id          serial      PRIMARY KEY,
    name        varchar(150),
    description varchar(250)
);

CREATE TABLE users (
    id              serial       PRIMARY KEY,
    user_role_id    int          REFERENCES user_roles(id),
    -- 'website' | 'api' — phân biệt nguồn gốc cũ
    source          varchar(10)  NOT NULL DEFAULT 'website',
    email           varchar(150),
    password_hash   varchar(250),
    phone           varchar(50),
    full_name       varchar(250),
    gender          char(1)      CHECK (gender IN ('M','F','U')),
    address         varchar(500),
    avatar          varchar(50),
    otp             varchar(50),
    ip_last_login   varchar(50),
    active          boolean,
    visible         boolean,
    created_at      timestamptz  NOT NULL DEFAULT now(),
    updated_at      timestamptz  NOT NULL DEFAULT now(),
    last_login_at   timestamptz
);

CREATE TABLE token_devices (
    id          serial      PRIMARY KEY,
    user_id     int         REFERENCES users(id),
    token       varchar(500),
    device_os   varchar(50),
    device_name varchar(500)
);

-- =============================================================================
-- 5. AGENTS
-- =============================================================================

CREATE TABLE agents (
    id                       serial       PRIMARY KEY,
    agent_code               varchar(50)  NOT NULL UNIQUE,
    name                     varchar(150) NOT NULL,
    address                  varchar(150),
    tel                      varchar(50),
    email                    varchar(150) NOT NULL,
    password_hash            varchar(250) NOT NULL,

    -- Galileo config
    galileo_pcc              varchar(10),
    galileo_hcm              varchar(10),
    galileo_connect_type     boolean      NOT NULL DEFAULT false,
    galileo_book_pcc         varchar(10),
    galileo_book_hcm         varchar(10),
    galileo_book_connect_type boolean     NOT NULL DEFAULT false,
    galileo_active           boolean      NOT NULL DEFAULT false,

    -- LCC / search config
    lcc_vn_active_domestic   boolean      NOT NULL DEFAULT false,
    lcc_vn_active_global     boolean      NOT NULL DEFAULT false,
    lcc_all_class            boolean      NOT NULL DEFAULT false,
    lcc_ignore_promo         boolean      NOT NULL DEFAULT false,

    -- Cache
    enable_cache             boolean      NOT NULL DEFAULT false,
    cache_time_minutes       int          NOT NULL DEFAULT 30,

    -- Misc
    default_currency         varchar(10),
    send_mail_in_api         boolean      NOT NULL DEFAULT false,
    email_sender_id          int,
    combined_mode            int          NOT NULL DEFAULT 0,
    baggage_fee_percent      numeric(10,4) NOT NULL DEFAULT 0,
    baggage_fee_amount       numeric(18,4) NOT NULL DEFAULT 0,

    active                   boolean      NOT NULL DEFAULT true,
    visible                  boolean      NOT NULL DEFAULT true,
    created_at               timestamptz  NOT NULL DEFAULT now(),
    expiry_date              timestamptz  NOT NULL
);

-- Agent ↔ PCC mapping
CREATE TABLE agent_pccs (
    id               serial      PRIMARY KEY,
    agent_id         int         NOT NULL REFERENCES agents(id),
    pcc              varchar(10) NOT NULL REFERENCES pccs(pcc) ON DELETE CASCADE,
    ignored_mode     int         NOT NULL DEFAULT 0,
    list_start_point text,
    active           boolean     NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX idx_agent_pccs_unique ON agent_pccs(agent_id, pcc);

-- Partners (Kiwi, Pkfare, Maybay...)
CREATE TABLE partners (
    id     serial      PRIMARY KEY,
    name   varchar(500),
    active boolean     NOT NULL DEFAULT true
);

-- Agent ↔ Partner mapping
CREATE TABLE agent_partners (
    id               serial  PRIMARY KEY,
    agent_id         int     NOT NULL REFERENCES agents(id),
    partner_id       int     NOT NULL REFERENCES partners(id),
    ignored_mode     int     NOT NULL DEFAULT 0,
    list_start_point text,
    active           boolean NOT NULL DEFAULT true
);
CREATE UNIQUE INDEX idx_agent_partners_unique ON agent_partners(agent_id, partner_id);

-- Per-agent LCC airline permissions
CREATE TABLE agent_lcc_info (
    agent_id     int         NOT NULL REFERENCES agents(id),
    airline_code varchar(10) NOT NULL,
    allow_search boolean     NOT NULL DEFAULT false,
    allow_book   boolean     NOT NULL DEFAULT false,
    PRIMARY KEY (agent_id, airline_code)
);

-- Per-agent airline ignore rules
CREATE TABLE agent_airline_ignores (
    id                     serial      PRIMARY KEY,
    agent_id               int         NOT NULL REFERENCES agents(id),
    airline_code           varchar(10) NOT NULL,
    filter_by_plating      boolean     NOT NULL DEFAULT false,
    filter_by_any_segment  boolean     NOT NULL DEFAULT false,
    filter_by_all_segments boolean     NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX idx_airline_ignore_unique ON agent_airline_ignores(agent_id, airline_code);

-- Commission matrix per agent
CREATE TABLE commissions (
    id                      serial       PRIMARY KEY,
    agent_id                int          NOT NULL REFERENCES agents(id),
    airline_group           varchar(10)  NOT NULL REFERENCES airline_types(type),
    start_region            varchar(10)  NOT NULL,  -- continent code
    end_region              varchar(10)  NOT NULL,
    currency                varchar(10)  NOT NULL DEFAULT 'VND',

    -- Service fees (cố định theo hành trình)
    fee_adt_one_way         numeric(18,4) NOT NULL DEFAULT 0,
    fee_chd_one_way         numeric(18,4) NOT NULL DEFAULT 0,
    fee_inf_one_way         numeric(18,4) NOT NULL DEFAULT 0,
    fee_adt_round_trip      numeric(18,4) NOT NULL DEFAULT 0,
    fee_chd_round_trip      numeric(18,4) NOT NULL DEFAULT 0,
    fee_inf_round_trip      numeric(18,4) NOT NULL DEFAULT 0,
    fee_by_percent          numeric(10,4) NOT NULL DEFAULT 0, -- % of fare

    -- Commission (deduct from base fare)
    commission              numeric(18,4) NOT NULL DEFAULT 0,
    com_by_percent          boolean       NOT NULL DEFAULT false,
    com_percent_on_base_fare boolean      NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX idx_commission_unique ON commissions(agent_id, airline_group, start_region, end_region);

-- =============================================================================
-- 6. BOOKINGS
-- =============================================================================

CREATE TABLE bookings (
    id                    serial       PRIMARY KEY,
    agent_id              int          REFERENCES agents(id),
    booking_code          varchar(50)  NOT NULL,
    booking_status        varchar(20)  NOT NULL DEFAULT 'PENDING',
                          -- PENDING | CONFIRMED | TICKETED | CANCELLED | EXPIRED

    -- Route
    departure_airport     varchar(10)  NOT NULL,
    destination_airport   varchar(10)  NOT NULL,
    departure_date        date,
    return_date           date,
    itinerary_type        int          NOT NULL DEFAULT 1, -- 1=oneway, 2=roundtrip
    airline_code          varchar(10)  NOT NULL,
    return_airline_code   varchar(10),

    -- Engine
    ibe_source            varchar(50),  -- 'galileo'|'datacom'|'kiwi'|'pkfare'|'maybay'
    pcc                   varchar(50),
    engine_session        text,         -- booking_token / SelectedValue / etc. (thay thế các cột engine-specific)

    -- Pricing  (đổi float → numeric)
    total_price           numeric(18,4) NOT NULL DEFAULT 0,
    total_service_fee     numeric(18,4) NOT NULL DEFAULT 0,
    grand_total           numeric(18,4) NOT NULL DEFAULT 0,
    currency_code         varchar(10),
    grand_total_usd       numeric(18,4) NOT NULL DEFAULT 0,
    vnd_usd_rate          numeric(18,6) NOT NULL DEFAULT 0,
    grand_total_origin    numeric(18,4) NOT NULL DEFAULT 0,
    origin_currency       varchar(10),
    origin_currency_usd_rate numeric(18,6) NOT NULL DEFAULT 0,

    -- Add-on totals
    total_baggage_price        numeric(18,4) NOT NULL DEFAULT 0,
    total_insurance_price      numeric(18,4) NOT NULL DEFAULT 0,
    total_travel_sim_price     numeric(18,4) NOT NULL DEFAULT 0,
    total_travel_insurance_price numeric(18,4) NOT NULL DEFAULT 0,
    total_car_rental_price     numeric(18,4) NOT NULL DEFAULT 0,
    total_trip_cancel_price    numeric(18,4) NOT NULL DEFAULT 0,
    total_trip_tour_price      numeric(18,4) NOT NULL DEFAULT 0,
    total_trip_visa_price      numeric(18,4) NOT NULL DEFAULT 0,
    total_trip_hotel_price     numeric(18,4) NOT NULL DEFAULT 0,

    -- Misc
    passenger_count       int,
    fare_data_id          int,
    departure_flight_id   int,
    return_flight_id      int,
    difference            numeric(18,4) NOT NULL DEFAULT 0,
    pay_with_agency_credit boolean      NOT NULL DEFAULT false,
    has_invoice           boolean       NOT NULL DEFAULT false,
    search_params         text,
    lang                  varchar(20),
    url_refer             varchar(500),
    remark                text,          -- was NText
    ip_address            varchar(50),
    visible               boolean       NOT NULL DEFAULT true,

    booked_at             timestamptz   NOT NULL DEFAULT now(),
    expiry_at             timestamptz,
    created_at            timestamptz   NOT NULL DEFAULT now(),
    updated_at            timestamptz   NOT NULL DEFAULT now()
);
CREATE INDEX idx_bookings_agent    ON bookings(agent_id);
CREATE INDEX idx_bookings_code     ON bookings(booking_code);
CREATE INDEX idx_bookings_status   ON bookings(booking_status);
CREATE INDEX idx_bookings_depart   ON bookings(departure_date);

-- Contact info tách ra (normalized)
CREATE TABLE booking_contacts (
    id           serial      PRIMARY KEY,
    booking_id   int         NOT NULL UNIQUE REFERENCES bookings(id) ON DELETE CASCADE,
    name         varchar(250),
    title        char(1)     CHECK (title IN ('M','F')), -- was bit ContactTitle
    email        varchar(250),
    phone        varchar(50),
    address      varchar(250),
    city         varchar(250),
    zip_code     varchar(50),
    country_name varchar(250)
);

-- Hotel fields tách ra (thay vì nhét vào tblBooking)
CREATE TABLE booking_hotels (
    id           serial      PRIMARY KEY,
    booking_id   int         NOT NULL UNIQUE REFERENCES bookings(id) ON DELETE CASCADE,
    hotel_no     varchar(100),
    hotel_name   text,
    hotel_room   text
);

-- Booking flights (tblBookingFlight)
CREATE TABLE booking_flights (
    id                serial      PRIMARY KEY,
    booking_id        int         NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    itinerary         int         NOT NULL DEFAULT 1, -- 1=depart, 2=return
    start_point       varchar(10),
    end_point         varchar(10),
    start_date        timestamptz NOT NULL,
    end_date          timestamptz NOT NULL,
    airline_code      varchar(10) NOT NULL,   -- fixed typo: AirrlineCode
    total_flight_time numeric(10,2) NOT NULL DEFAULT 0,
    flight_number     varchar(100)
);
CREATE INDEX idx_booking_flights_booking ON booking_flights(booking_id);

-- Booking segments (tblBookingSegment)
CREATE TABLE booking_segments (
    id                     serial       PRIMARY KEY,
    flight_id              int          NOT NULL REFERENCES booking_flights(id) ON DELETE CASCADE,
    departure_date         timestamptz  NOT NULL,
    arrival_date           timestamptz  NOT NULL,
    class                  varchar(50),
    start_terminal         varchar(10),
    end_terminal           varchar(10),
    departure_city_code    varchar(10),
    destination_city_code  varchar(10),
    departure_airport_code varchar(10),
    destination_airport_code varchar(10),
    airline_code           varchar(10),
    op_airline_code        varchar(10),
    flight_no              varchar(50),
    flight_duration        numeric(10,2) NOT NULL DEFAULT 0,
    aircraft_iata          varchar(10),
    items_no               int          NOT NULL DEFAULT 1,
    stop_point             varchar(10),
    stop_time              numeric(10,2) NOT NULL DEFAULT 0,
    is_last_item           boolean      NOT NULL DEFAULT false,
    different_day          boolean      NOT NULL DEFAULT false,
    overnight_stop         boolean      NOT NULL DEFAULT false,
    change_terminal        boolean      NOT NULL DEFAULT false,
    change_airport         boolean      NOT NULL DEFAULT false,
    transit                boolean      NOT NULL DEFAULT false,
    airport_change_to      varchar(10),
    frst_dwnln_stp         varchar(50),
    last_dwnln_stp         varchar(50)
);
CREATE INDEX idx_booking_segments_flight ON booking_segments(flight_id);

-- Passengers (tblPassenger)
CREATE TABLE passengers (
    id                      serial       PRIMARY KEY,
    booking_id              int          REFERENCES bookings(id) ON DELETE CASCADE,
    passenger_type          varchar(10),  -- ADT|CHD|INF (refs passenger_types)
    first_name              varchar(150),
    last_name               varchar(150),
    gender                  char(1)      CHECK (gender IN ('M','F','U')),
    birthday                date,
    passport_number         varchar(50),
    passport_expiry_date    date,
    nationality             varchar(150),

    -- Pricing per pax  (numeric thay float)
    fare                    numeric(18,4) NOT NULL DEFAULT 0,
    tax                     numeric(18,4) NOT NULL DEFAULT 0,
    fee                     numeric(18,4) NOT NULL DEFAULT 0,
    service_fee             numeric(18,4) NOT NULL DEFAULT 0,
    price                   numeric(18,4) NOT NULL DEFAULT 0,

    -- Ancillaries
    travel_sim_code         varchar(50),
    travel_sim_price        numeric(18,4) NOT NULL DEFAULT 0,
    travel_insurance_code   varchar(50),
    travel_insurance_price  numeric(18,4) NOT NULL DEFAULT 0
);
CREATE INDEX idx_passengers_booking ON passengers(booking_id);

-- Baggages
CREATE TABLE baggages (
    id           serial       PRIMARY KEY,
    passenger_id int          NOT NULL REFERENCES passengers(id) ON DELETE CASCADE,
    booking_id   int          NOT NULL REFERENCES bookings(id)  ON DELETE CASCADE,
    baggage      varchar(250),
    value        varchar(50),
    price        numeric(18,4) NOT NULL DEFAULT 0
);

-- Insurance
CREATE TABLE insurances (
    id           serial       PRIMARY KEY,
    passenger_id int          NOT NULL REFERENCES passengers(id) ON DELETE CASCADE,
    booking_id   int          NOT NULL REFERENCES bookings(id)  ON DELETE CASCADE,
    name         varchar(250),
    value        varchar(50),
    price        numeric(18,4) NOT NULL DEFAULT 0
);

-- Invoice
CREATE TABLE invoices (
    id           serial       PRIMARY KEY,
    booking_id   int          NOT NULL UNIQUE REFERENCES bookings(id) ON DELETE CASCADE,
    country_code varchar(10)  NOT NULL,
    company_name varchar(250),
    address      varchar(500),
    postal_code  varchar(50),
    city_name    varchar(250),
    tax_code     varchar(50),
    receiver     varchar(250)
);

-- Issued tickets
CREATE TABLE tickets (
    id             serial       PRIMARY KEY,
    airline_code   varchar(10)  NOT NULL,
    booking_code   varchar(50)  NOT NULL,
    passenger_name varchar(250) NOT NULL,
    ticket_number  varchar(50)  NOT NULL,
    response       text,        -- was NText
    issued_at      timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX idx_tickets_booking ON tickets(booking_code);

-- =============================================================================
-- 7. ADD-ON SERVICES
-- =============================================================================

CREATE TABLE trip_cancellations (
    id             serial       PRIMARY KEY,
    booking_id     int          NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    markup_amount  numeric(18,4) NOT NULL DEFAULT 0,
    markup_percent numeric(10,4) NOT NULL DEFAULT 0,
    price          numeric(18,4) NOT NULL DEFAULT 0,
    booking_price  numeric(18,4) NOT NULL DEFAULT 0,
    currency       varchar(20),
    value          text
);

CREATE TABLE trip_tours (
    id         serial       PRIMARY KEY,
    booking_id int          NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    code       varchar(250),
    name       text,
    price      numeric(18,4),
    value      text,
    currency   varchar(50)
);

CREATE TABLE trip_visas (
    id         serial       PRIMARY KEY,
    booking_id int          NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    code       varchar(250),
    name       text,
    price      numeric(18,4),
    price_vn   numeric(18,4),
    value      text,
    currency   varchar(50)
);

CREATE TABLE car_rentals (
    id                serial       PRIMARY KEY,
    booking_id        int          REFERENCES bookings(id),
    code              varchar(50),
    name              text,
    price             numeric(18,4) NOT NULL DEFAULT 0,
    price_vnd         numeric(18,4) NOT NULL DEFAULT 0,
    currency_code     varchar(50),
    catch_in_house    int          NOT NULL DEFAULT 0,
    chunk_id          int,
    dimension_id      int,
    vehicle_id        int,
    ride_method_id    int,
    pm_id             int,
    brand_partner_id  int,
    airport_id        int,
    street_id         int,
    village_id        int,
    city_id           int,
    airport_code      text,
    pick_pos          text,
    drop_pos          text,
    pick_address      text,
    drop_address      text,
    depart_time       text,
    plane_time        text,
    plane_number      text,
    items_no          int
);

-- =============================================================================
-- 8. ANALYTICS (giữ lại, bỏ tblCache + tblConfigSystemLog)
-- =============================================================================

CREATE TABLE search_analytics (
    id           serial      PRIMARY KEY,
    agent_id     int         NOT NULL REFERENCES agents(id),
    start_point  varchar(50),
    end_point    varchar(50),
    itinerary    int,
    depart_date  date,
    return_date  date,
    flight_type  boolean,    -- domestic/international
    ip_address   varchar(50),
    searched_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_search_analytics_agent ON search_analytics(agent_id);
CREATE INDEX idx_search_analytics_time  ON search_analytics(searched_at);

CREATE TABLE search_details (
    id           serial      PRIMARY KEY,
    agent_id     int         NOT NULL REFERENCES agents(id),
    start_point  varchar(50),
    end_point    varchar(50),
    itinerary    int,
    depart_date  date,
    return_date  date,
    ibe_source   varchar(50),
    ip_address   varchar(50),
    searched_at  timestamptz NOT NULL DEFAULT now()
);

-- =============================================================================
-- 9. OUTBOX (Transactional Outbox Pattern — mới, không có trong schema cũ)
-- =============================================================================

CREATE TABLE outbox_messages (
    id              uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    type            varchar(500) NOT NULL,
    content         text         NOT NULL,
    occurred_on_utc timestamptz  NOT NULL,
    processed_on_utc timestamptz,
    error           text,
    retry_count     int          NOT NULL DEFAULT 0,
    locked_until    timestamptz
);
CREATE INDEX idx_outbox_unprocessed ON outbox_messages(processed_on_utc) WHERE processed_on_utc IS NULL;

-- =============================================================================
-- refresh_tokens — Self-hosted JWT refresh token store
-- Keycloak migration: this table can be dropped; Keycloak manages sessions.
-- =============================================================================

CREATE TABLE refresh_tokens (
    id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    agent_id   int         NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    token      text        NOT NULL UNIQUE,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_refresh_tokens_token     ON refresh_tokens(token);
CREATE INDEX idx_refresh_tokens_agent_id  ON refresh_tokens(agent_id);
CREATE INDEX idx_refresh_tokens_expires   ON refresh_tokens(expires_at) WHERE revoked_at IS NULL;
