using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixIdColumnNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_accounts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trip_visas",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trip_tours",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "trip_cancellations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "search_analytics",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "pccs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "partners",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "lcc_infos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "insurances",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "geo_countries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "geo_continents",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "geo_cities",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "geo_airports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "class_and_notes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "car_rentals",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "baggages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "airlines",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "airline_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "airline_ignores",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "aircrafts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "agent_pccs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "agent_partners",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "user_roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "user_accounts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "trip_visas",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "trip_tours",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "trip_cancellations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "search_analytics",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "pccs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "partners",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "lcc_infos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "invoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "insurances",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "geo_countries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "geo_continents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "geo_cities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "geo_airports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "class_and_notes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "car_rentals",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "baggages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "airlines",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "airline_types",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "airline_ignores",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "aircrafts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "agent_pccs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "agent_partners",
                newName: "Id");
        }
    }
}
