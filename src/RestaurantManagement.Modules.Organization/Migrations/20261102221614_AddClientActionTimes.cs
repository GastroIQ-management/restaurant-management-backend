using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Modules.Organization.Migrations
{
    /// <inheritdoc />
    public partial class AddClientActionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organizations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organizations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_settings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_settings",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "locations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "locations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_settings");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_settings");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_settings");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_settings");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_settings");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "locations");
        }
    }
}
