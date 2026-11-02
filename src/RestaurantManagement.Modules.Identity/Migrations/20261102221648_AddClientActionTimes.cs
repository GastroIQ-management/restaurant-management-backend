using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Modules.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddClientActionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_roles",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_role_levels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_role_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_role_levels",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_role_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_role_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_members",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_invitations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_invitations",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_invitation_roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_invitation_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_invitation_roles",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_invitation_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_invitation_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "organization_invitation_levels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "organization_invitation_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "organization_invitation_levels",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "organization_invitation_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "organization_invitation_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "member_location_roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "member_location_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "member_location_roles",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "member_location_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "member_location_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "client_action_offset_minutes",
                table: "member_location_levels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "client_action_time",
                table: "member_location_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "clock_error",
                table: "member_location_levels",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "corrected_action_time",
                table: "member_location_levels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "received_at",
                table: "member_location_levels",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_roles");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_roles");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_roles");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_roles");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_roles");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_role_levels");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_role_levels");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_role_levels");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_role_levels");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_role_levels");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_members");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_invitations");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_invitation_roles");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_invitation_roles");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_invitation_roles");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_invitation_roles");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_invitation_roles");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "organization_invitation_levels");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "organization_invitation_levels");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "organization_invitation_levels");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "organization_invitation_levels");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "organization_invitation_levels");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "member_location_roles");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "member_location_roles");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "member_location_roles");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "member_location_roles");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "member_location_roles");

            migrationBuilder.DropColumn(
                name: "client_action_offset_minutes",
                table: "member_location_levels");

            migrationBuilder.DropColumn(
                name: "client_action_time",
                table: "member_location_levels");

            migrationBuilder.DropColumn(
                name: "clock_error",
                table: "member_location_levels");

            migrationBuilder.DropColumn(
                name: "corrected_action_time",
                table: "member_location_levels");

            migrationBuilder.DropColumn(
                name: "received_at",
                table: "member_location_levels");
        }
    }
}
