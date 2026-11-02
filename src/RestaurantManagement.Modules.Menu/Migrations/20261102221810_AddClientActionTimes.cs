using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Modules.Menu.Migrations
{
    /// <inheritdoc />
    public partial class AddClientActionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "menus",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "menus",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "menu_categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "menu_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "menu_categories",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "menu_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "menu_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "location_menus",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "location_menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "location_menus",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "location_menus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "location_menus",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "location_menus");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "location_menus");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "location_menus");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "location_menus");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "location_menus");
        }
    }
}
