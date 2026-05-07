using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixListStartPointLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "list_start_point",
                table: "agent_pccs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "list_start_point",
                table: "agent_partners",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "list_start_point",
                table: "agent_pccs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "list_start_point",
                table: "agent_partners",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
