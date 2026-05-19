using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTicketRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_bookings_BookingId1",
                table: "tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_tickets_bookings_booking_id",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_BookingId1",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "BookingId1",
                table: "tickets");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_bookings_booking_id",
                table: "tickets",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_bookings_booking_id",
                table: "tickets");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId1",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_BookingId1",
                table: "tickets",
                column: "BookingId1");

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
    }
}
