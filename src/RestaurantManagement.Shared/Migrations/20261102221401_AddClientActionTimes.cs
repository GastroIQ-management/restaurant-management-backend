using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddClientActionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "audit_log_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "audit_log_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "audit_log_entries",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "audit_log_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "audit_log_entries",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "audit_log_entries");
        }
    }
}
