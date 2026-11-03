using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantManagement.Shared.Data.Changes;

#nullable disable

namespace RestaurantManagement.Modules.Organization.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationChangeStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "change_stamp",
                table: "locations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_locations_organization_id_change_stamp",
                table: "locations",
                columns: new[] { "organization_id", "change_stamp" });

            // Hand-written trigger (approved at plan Gate 1): EF cannot stamp updates with the transaction ID.
            migrationBuilder.Sql(ChangeTrackingSql.StampTrigger("organization", "locations"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS organization_locations_stamp ON locations; DROP FUNCTION IF EXISTS organization_locations_stamp();");

            migrationBuilder.DropIndex(
                name: "ix_locations_organization_id_change_stamp",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "change_stamp",
                table: "locations");
        }
    }
}
