using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Flight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FullSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingFlights_bookings_BookingId",
                table: "BookingFlights");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSegments_BookingFlights_BookingFlightEntityId",
                table: "BookingSegments");

            migrationBuilder.DropForeignKey(
                name: "FK_passengers_bookings_BookingId",
                table: "passengers");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_bookings_BookingId",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingSegments",
                table: "BookingSegments");

            migrationBuilder.DropIndex(
                name: "IX_BookingSegments_BookingFlightEntityId",
                table: "BookingSegments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingFlights",
                table: "BookingFlights");

            migrationBuilder.DropColumn(
                name: "BookingFlightEntityId",
                table: "BookingSegments");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "tickets");

            migrationBuilder.RenameTable(
                name: "BookingSegments",
                newName: "booking_segments");

            migrationBuilder.RenameTable(
                name: "BookingFlights",
                newName: "booking_flights");

            migrationBuilder.RenameColumn(
                name: "Airline",
                table: "tickets",
                newName: "airline");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tickets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TicketNumber",
                table: "tickets",
                newName: "ticket_number");

            migrationBuilder.RenameColumn(
                name: "PassengerName",
                table: "tickets",
                newName: "passenger_name");

            migrationBuilder.RenameColumn(
                name: "PassengerId",
                table: "tickets",
                newName: "passenger_id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOnUtc",
                table: "tickets",
                newName: "last_modified_on_utc");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "tickets",
                newName: "issued_at");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "tickets",
                newName: "created_on_utc");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "tickets",
                newName: "booking_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_BookingId",
                table: "tickets",
                newName: "IX_tickets_booking_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "passengers",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Nationality",
                table: "passengers",
                newName: "nationality");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "passengers",
                newName: "gender");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "passengers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PassportNo",
                table: "passengers",
                newName: "passport_no");

            migrationBuilder.RenameColumn(
                name: "PassportExpiry",
                table: "passengers",
                newName: "passport_expiry");

            migrationBuilder.RenameColumn(
                name: "MiddleName",
                table: "passengers",
                newName: "middle_name");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "passengers",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOnUtc",
                table: "passengers",
                newName: "last_modified_on_utc");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "passengers",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "FareCurrency",
                table: "passengers",
                newName: "fare_currency");

            migrationBuilder.RenameColumn(
                name: "FareAmount",
                table: "passengers",
                newName: "fare_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "passengers",
                newName: "created_on_utc");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "passengers",
                newName: "booking_id");

            migrationBuilder.RenameColumn(
                name: "BirthDate",
                table: "passengers",
                newName: "birth_date");

            migrationBuilder.RenameColumn(
                name: "BaggageKg",
                table: "passengers",
                newName: "baggage_kg");

            migrationBuilder.RenameIndex(
                name: "IX_passengers_BookingId",
                table: "passengers",
                newName: "IX_passengers_booking_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "bookings",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "bookings",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Origin",
                table: "bookings",
                newName: "origin");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "bookings",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "bookings",
                newName: "currency");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "bookings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TripType",
                table: "bookings",
                newName: "trip_type");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "bookings",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "bookings",
                newName: "session_id");

            migrationBuilder.RenameColumn(
                name: "ServiceFee",
                table: "bookings",
                newName: "service_fee");

            migrationBuilder.RenameColumn(
                name: "ReturnDate",
                table: "bookings",
                newName: "return_date");

            migrationBuilder.RenameColumn(
                name: "PccCode",
                table: "bookings",
                newName: "pcc_code");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOnUtc",
                table: "bookings",
                newName: "last_modified_on_utc");

            migrationBuilder.RenameColumn(
                name: "FareId",
                table: "bookings",
                newName: "fare_id");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "bookings",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "DepartDate",
                table: "bookings",
                newName: "depart_date");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "bookings",
                newName: "created_on_utc");

            migrationBuilder.RenameColumn(
                name: "ContactPhone",
                table: "bookings",
                newName: "contact_phone");

            migrationBuilder.RenameColumn(
                name: "ContactName",
                table: "bookings",
                newName: "contact_name");

            migrationBuilder.RenameColumn(
                name: "ContactEmail",
                table: "bookings",
                newName: "contact_email");

            migrationBuilder.RenameColumn(
                name: "BookingCode",
                table: "bookings",
                newName: "booking_code");

            migrationBuilder.RenameColumn(
                name: "AgentCode",
                table: "bookings",
                newName: "agent_code");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_Status",
                table: "bookings",
                newName: "IX_bookings_status");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_CreatedOnUtc",
                table: "bookings",
                newName: "IX_bookings_created_on_utc");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_BookingCode",
                table: "bookings",
                newName: "IX_bookings_booking_code");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_AgentCode",
                table: "bookings",
                newName: "IX_bookings_agent_code");

            migrationBuilder.RenameColumn(
                name: "Origin",
                table: "booking_segments",
                newName: "origin");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "booking_segments",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Airline",
                table: "booking_segments",
                newName: "airline");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "booking_segments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOnUtc",
                table: "booking_segments",
                newName: "last_modified_on_utc");

            migrationBuilder.RenameColumn(
                name: "FlightNumber",
                table: "booking_segments",
                newName: "flight_number");

            migrationBuilder.RenameColumn(
                name: "DepartTime",
                table: "booking_segments",
                newName: "depart_time");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "booking_segments",
                newName: "created_on_utc");

            migrationBuilder.RenameColumn(
                name: "CabinClass",
                table: "booking_segments",
                newName: "cabin_class");

            migrationBuilder.RenameColumn(
                name: "BookingFlightId",
                table: "booking_segments",
                newName: "booking_flight_id");

            migrationBuilder.RenameColumn(
                name: "ArriveTime",
                table: "booking_segments",
                newName: "arrive_time");

            migrationBuilder.RenameColumn(
                name: "AircraftType",
                table: "booking_segments",
                newName: "aircraft_type");

            migrationBuilder.RenameColumn(
                name: "Origin",
                table: "booking_flights",
                newName: "origin");

            migrationBuilder.RenameColumn(
                name: "Destination",
                table: "booking_flights",
                newName: "destination");

            migrationBuilder.RenameColumn(
                name: "Airline",
                table: "booking_flights",
                newName: "airline");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "booking_flights",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StopCount",
                table: "booking_flights",
                newName: "stop_count");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOnUtc",
                table: "booking_flights",
                newName: "last_modified_on_utc");

            migrationBuilder.RenameColumn(
                name: "DepartTime",
                table: "booking_flights",
                newName: "depart_time");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                table: "booking_flights",
                newName: "created_on_utc");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "booking_flights",
                newName: "booking_id");

            migrationBuilder.RenameColumn(
                name: "ArriveTime",
                table: "booking_flights",
                newName: "arrive_time");

            migrationBuilder.RenameIndex(
                name: "IX_BookingFlights_BookingId",
                table: "booking_flights",
                newName: "IX_booking_flights_booking_id");

            migrationBuilder.AlterColumn<string>(
                name: "airline",
                table: "tickets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ticket_number",
                table: "tickets",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "passenger_name",
                table: "tickets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "baggage_kg",
                table: "passengers",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "passengers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "passengers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "outbox_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "origin",
                table: "booking_segments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination",
                table: "booking_segments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "airline",
                table: "booking_segments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "flight_number",
                table: "booking_segments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "cabin_class",
                table: "booking_segments",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "aircraft_type",
                table: "booking_segments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "booking_segments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "booking_segments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "origin",
                table: "booking_flights",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "destination",
                table: "booking_flights",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "airline",
                table: "booking_flights",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "booking_flights",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "booking_flights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_tickets",
                table: "tickets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_booking_segments",
                table: "booking_segments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_booking_flights",
                table: "booking_flights",
                column: "id");

            migrationBuilder.CreateTable(
                name: "agent_partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_id = table.Column<int>(type: "integer", nullable: false),
                    partner_id = table.Column<int>(type: "integer", nullable: false),
                    ignored_mode = table.Column<int>(type: "integer", nullable: false),
                    list_start_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "agent_pccs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_id = table.Column<int>(type: "integer", nullable: false),
                    pcc = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ignored_mode = table.Column<int>(type: "integer", nullable: false),
                    list_start_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_pccs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    lcc_vn_active_domestic = table.Column<bool>(type: "boolean", nullable: false),
                    lcc_vn_active_global = table.Column<bool>(type: "boolean", nullable: false),
                    galileo_pcc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    galileo_active = table.Column<bool>(type: "boolean", nullable: false),
                    default_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    enable_cache = table.Column<bool>(type: "boolean", nullable: false),
                    cache_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    send_mail_in_api = table.Column<bool>(type: "boolean", nullable: false),
                    email_sender = table.Column<int>(type: "integer", nullable: false),
                    combined_mode = table.Column<int>(type: "integer", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    baggage_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    baggage_fee_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "aircrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    iata = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aircrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airline_ignores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_id = table.Column<int>(type: "integer", nullable: false),
                    airline = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    filter_by_plating_carrier = table.Column<bool>(type: "boolean", nullable: false),
                    filter_by_any_segment = table.Column<bool>(type: "boolean", nullable: false),
                    filter_by_all_segment = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_ignores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airline_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airline_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "airlines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "baggages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    baggage_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    flight_id = table.Column<int>(type: "integer", nullable: true),
                    flight_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pax_id = table.Column<int>(type: "integer", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    weight = table.Column<int>(type: "integer", nullable: true),
                    weight_unit = table.Column<int>(type: "integer", nullable: true),
                    piece_allowance = table.Column<int>(type: "integer", nullable: true),
                    cabin_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    baggage_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_baggages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "car_rentals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pickup_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dropoff_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pickup_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dropoff_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    car_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    car_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    daily_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_days = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_car_rentals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_and_notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    airline_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    class_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    show_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    non_refundable = table.Column<bool>(type: "boolean", nullable: false),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    start_airport_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    end_airport_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    start_city_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    end_city_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    start_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    end_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    start_continent_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    end_continent_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_and_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "geo_continents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_fr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_continents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "insurances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    policy_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    coverage_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    premium = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    beneficiary_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    beneficiary_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insurances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    city_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    receiver = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiver_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    receiver_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lcc_infos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_id = table.Column<int>(type: "integer", nullable: false),
                    airline = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    allow_search = table.Column<bool>(type: "boolean", nullable: false),
                    allow_book = table.Column<bool>(type: "boolean", nullable: false),
                    proxy_server_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    proxy_server_book_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lcc_infos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "passenger_types",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    name_fr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passenger_types", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "pccs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pccs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "search_analytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    agent_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_point = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    end_point = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    itinerary = table.Column<int>(type: "integer", nullable: false),
                    depart_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    flight_type = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sources = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_analytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trip_cancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    markup_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    markup_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    booking_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_cancellations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trip_tours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tour_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tour_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    departure_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pax_count = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trip_visas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    price_vn = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_visas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_role_id = table.Column<int>(type: "integer", nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    password = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gender = table.Column<bool>(type: "boolean", nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_last_login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    otp = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "geo_countries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    continent_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_fr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    flag = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GeoContinentId = table.Column<string>(type: "text", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_countries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_geo_countries_geo_continents_GeoContinentId",
                        column: x => x.GeoContinentId,
                        principalTable: "geo_continents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_geo_countries_geo_continents_continent_code",
                        column: x => x.continent_code,
                        principalTable: "geo_continents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "geo_cities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_fr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    search_keys = table.Column<string>(type: "text", nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GeoCountryId = table.Column<string>(type: "text", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_geo_cities_geo_countries_GeoCountryId",
                        column: x => x.GeoCountryId,
                        principalTable: "geo_countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_geo_cities_geo_countries_country_code",
                        column: x => x.country_code,
                        principalTable: "geo_countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "geo_airports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    city_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name_vi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_fr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    location = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    search_keys = table.Column<string>(type: "text", nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    GeoCityId = table.Column<string>(type: "text", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geo_airports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_geo_airports_geo_cities_GeoCityId",
                        column: x => x.GeoCityId,
                        principalTable: "geo_cities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_geo_airports_geo_cities_city_code",
                        column: x => x.city_code,
                        principalTable: "geo_cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_BookingId1",
                table: "tickets",
                column: "BookingId1");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ticket_number",
                table: "tickets",
                column: "ticket_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_segments_booking_flight_id",
                table: "booking_segments",
                column: "booking_flight_id");

            migrationBuilder.CreateIndex(
                name: "IX_agents_agent_code",
                table: "agents",
                column: "agent_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airline_types_code",
                table: "airline_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airlines_code",
                table: "airlines",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_geo_airports_city_code",
                table: "geo_airports",
                column: "city_code");

            migrationBuilder.CreateIndex(
                name: "IX_geo_airports_GeoCityId",
                table: "geo_airports",
                column: "GeoCityId");

            migrationBuilder.CreateIndex(
                name: "IX_geo_cities_country_code",
                table: "geo_cities",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "IX_geo_cities_GeoCountryId",
                table: "geo_cities",
                column: "GeoCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_geo_countries_continent_code",
                table: "geo_countries",
                column: "continent_code");

            migrationBuilder.CreateIndex(
                name: "IX_geo_countries_GeoContinentId",
                table: "geo_countries",
                column: "GeoContinentId");

            migrationBuilder.AddForeignKey(
                name: "FK_booking_flights_bookings_booking_id",
                table: "booking_flights",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_booking_segments_booking_flights_booking_flight_id",
                table: "booking_segments",
                column: "booking_flight_id",
                principalTable: "booking_flights",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_passengers_bookings_booking_id",
                table: "passengers",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_bookings_BookingId1",
                table: "tickets",
                column: "BookingId1",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_bookings_booking_id",
                table: "tickets",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_booking_flights_bookings_booking_id",
                table: "booking_flights");

            migrationBuilder.DropForeignKey(
                name: "FK_booking_segments_booking_flights_booking_flight_id",
                table: "booking_segments");

            migrationBuilder.DropForeignKey(
                name: "FK_passengers_bookings_booking_id",
                table: "passengers");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_bookings_BookingId1",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_bookings_booking_id",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "agent_partners");

            migrationBuilder.DropTable(
                name: "agent_pccs");

            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "aircrafts");

            migrationBuilder.DropTable(
                name: "airline_ignores");

            migrationBuilder.DropTable(
                name: "airline_types");

            migrationBuilder.DropTable(
                name: "airlines");

            migrationBuilder.DropTable(
                name: "baggages");

            migrationBuilder.DropTable(
                name: "car_rentals");

            migrationBuilder.DropTable(
                name: "class_and_notes");

            migrationBuilder.DropTable(
                name: "geo_airports");

            migrationBuilder.DropTable(
                name: "insurances");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "lcc_infos");

            migrationBuilder.DropTable(
                name: "partners");

            migrationBuilder.DropTable(
                name: "passenger_types");

            migrationBuilder.DropTable(
                name: "pccs");

            migrationBuilder.DropTable(
                name: "search_analytics");

            migrationBuilder.DropTable(
                name: "trip_cancellations");

            migrationBuilder.DropTable(
                name: "trip_tours");

            migrationBuilder.DropTable(
                name: "trip_visas");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "geo_cities");

            migrationBuilder.DropTable(
                name: "geo_countries");

            migrationBuilder.DropTable(
                name: "geo_continents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tickets",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_BookingId1",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_ticket_number",
                table: "tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_booking_segments",
                table: "booking_segments");

            migrationBuilder.DropIndex(
                name: "IX_booking_segments_booking_flight_id",
                table: "booking_segments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_booking_flights",
                table: "booking_flights");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "passengers");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "passengers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "booking_segments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "booking_segments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "booking_flights");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "booking_flights");

            migrationBuilder.RenameTable(
                name: "tickets",
                newName: "Tickets");

            migrationBuilder.RenameTable(
                name: "booking_segments",
                newName: "BookingSegments");

            migrationBuilder.RenameTable(
                name: "booking_flights",
                newName: "BookingFlights");

            migrationBuilder.RenameColumn(
                name: "airline",
                table: "Tickets",
                newName: "Airline");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tickets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ticket_number",
                table: "Tickets",
                newName: "TicketNumber");

            migrationBuilder.RenameColumn(
                name: "passenger_name",
                table: "Tickets",
                newName: "PassengerName");

            migrationBuilder.RenameColumn(
                name: "passenger_id",
                table: "Tickets",
                newName: "PassengerId");

            migrationBuilder.RenameColumn(
                name: "last_modified_on_utc",
                table: "Tickets",
                newName: "LastModifiedOnUtc");

            migrationBuilder.RenameColumn(
                name: "issued_at",
                table: "Tickets",
                newName: "IssuedAt");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                table: "Tickets",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "Tickets",
                newName: "BookingId");

            migrationBuilder.RenameIndex(
                name: "IX_tickets_booking_id",
                table: "Tickets",
                newName: "IX_Tickets_BookingId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "passengers",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "nationality",
                table: "passengers",
                newName: "Nationality");

            migrationBuilder.RenameColumn(
                name: "gender",
                table: "passengers",
                newName: "Gender");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "passengers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "passport_no",
                table: "passengers",
                newName: "PassportNo");

            migrationBuilder.RenameColumn(
                name: "passport_expiry",
                table: "passengers",
                newName: "PassportExpiry");

            migrationBuilder.RenameColumn(
                name: "middle_name",
                table: "passengers",
                newName: "MiddleName");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "passengers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "last_modified_on_utc",
                table: "passengers",
                newName: "LastModifiedOnUtc");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "passengers",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "fare_currency",
                table: "passengers",
                newName: "FareCurrency");

            migrationBuilder.RenameColumn(
                name: "fare_amount",
                table: "passengers",
                newName: "FareAmount");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                table: "passengers",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "passengers",
                newName: "BookingId");

            migrationBuilder.RenameColumn(
                name: "birth_date",
                table: "passengers",
                newName: "BirthDate");

            migrationBuilder.RenameColumn(
                name: "baggage_kg",
                table: "passengers",
                newName: "BaggageKg");

            migrationBuilder.RenameIndex(
                name: "IX_passengers_booking_id",
                table: "passengers",
                newName: "IX_passengers_BookingId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "bookings",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "bookings",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "origin",
                table: "bookings",
                newName: "Origin");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "bookings",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "currency",
                table: "bookings",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "bookings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "trip_type",
                table: "bookings",
                newName: "TripType");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "bookings",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "session_id",
                table: "bookings",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "service_fee",
                table: "bookings",
                newName: "ServiceFee");

            migrationBuilder.RenameColumn(
                name: "return_date",
                table: "bookings",
                newName: "ReturnDate");

            migrationBuilder.RenameColumn(
                name: "pcc_code",
                table: "bookings",
                newName: "PccCode");

            migrationBuilder.RenameColumn(
                name: "last_modified_on_utc",
                table: "bookings",
                newName: "LastModifiedOnUtc");

            migrationBuilder.RenameColumn(
                name: "fare_id",
                table: "bookings",
                newName: "FareId");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "bookings",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "depart_date",
                table: "bookings",
                newName: "DepartDate");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                table: "bookings",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "contact_phone",
                table: "bookings",
                newName: "ContactPhone");

            migrationBuilder.RenameColumn(
                name: "contact_name",
                table: "bookings",
                newName: "ContactName");

            migrationBuilder.RenameColumn(
                name: "contact_email",
                table: "bookings",
                newName: "ContactEmail");

            migrationBuilder.RenameColumn(
                name: "booking_code",
                table: "bookings",
                newName: "BookingCode");

            migrationBuilder.RenameColumn(
                name: "agent_code",
                table: "bookings",
                newName: "AgentCode");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_status",
                table: "bookings",
                newName: "IX_bookings_Status");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_created_on_utc",
                table: "bookings",
                newName: "IX_bookings_CreatedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_booking_code",
                table: "bookings",
                newName: "IX_bookings_BookingCode");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_agent_code",
                table: "bookings",
                newName: "IX_bookings_AgentCode");

            migrationBuilder.RenameColumn(
                name: "origin",
                table: "BookingSegments",
                newName: "Origin");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "BookingSegments",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "airline",
                table: "BookingSegments",
                newName: "Airline");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BookingSegments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_modified_on_utc",
                table: "BookingSegments",
                newName: "LastModifiedOnUtc");

            migrationBuilder.RenameColumn(
                name: "flight_number",
                table: "BookingSegments",
                newName: "FlightNumber");

            migrationBuilder.RenameColumn(
                name: "depart_time",
                table: "BookingSegments",
                newName: "DepartTime");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                table: "BookingSegments",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "cabin_class",
                table: "BookingSegments",
                newName: "CabinClass");

            migrationBuilder.RenameColumn(
                name: "booking_flight_id",
                table: "BookingSegments",
                newName: "BookingFlightId");

            migrationBuilder.RenameColumn(
                name: "arrive_time",
                table: "BookingSegments",
                newName: "ArriveTime");

            migrationBuilder.RenameColumn(
                name: "aircraft_type",
                table: "BookingSegments",
                newName: "AircraftType");

            migrationBuilder.RenameColumn(
                name: "origin",
                table: "BookingFlights",
                newName: "Origin");

            migrationBuilder.RenameColumn(
                name: "destination",
                table: "BookingFlights",
                newName: "Destination");

            migrationBuilder.RenameColumn(
                name: "airline",
                table: "BookingFlights",
                newName: "Airline");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BookingFlights",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "stop_count",
                table: "BookingFlights",
                newName: "StopCount");

            migrationBuilder.RenameColumn(
                name: "last_modified_on_utc",
                table: "BookingFlights",
                newName: "LastModifiedOnUtc");

            migrationBuilder.RenameColumn(
                name: "depart_time",
                table: "BookingFlights",
                newName: "DepartTime");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                table: "BookingFlights",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "BookingFlights",
                newName: "BookingId");

            migrationBuilder.RenameColumn(
                name: "arrive_time",
                table: "BookingFlights",
                newName: "ArriveTime");

            migrationBuilder.RenameIndex(
                name: "IX_booking_flights_booking_id",
                table: "BookingFlights",
                newName: "IX_BookingFlights_BookingId");

            migrationBuilder.AlterColumn<string>(
                name: "Airline",
                table: "Tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "TicketNumber",
                table: "Tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "PassengerName",
                table: "Tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaggageKg",
                table: "passengers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "BookingSegments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Destination",
                table: "BookingSegments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Airline",
                table: "BookingSegments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "FlightNumber",
                table: "BookingSegments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "CabinClass",
                table: "BookingSegments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.AlterColumn<string>(
                name: "AircraftType",
                table: "BookingSegments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingFlightEntityId",
                table: "BookingSegments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "BookingFlights",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Destination",
                table: "BookingFlights",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Airline",
                table: "BookingFlights",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingSegments",
                table: "BookingSegments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingFlights",
                table: "BookingFlights",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSegments_BookingFlightEntityId",
                table: "BookingSegments",
                column: "BookingFlightEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingFlights_bookings_BookingId",
                table: "BookingFlights",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSegments_BookingFlights_BookingFlightEntityId",
                table: "BookingSegments",
                column: "BookingFlightEntityId",
                principalTable: "BookingFlights",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_passengers_bookings_BookingId",
                table: "passengers",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_bookings_BookingId",
                table: "Tickets",
                column: "BookingId",
                principalTable: "bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
