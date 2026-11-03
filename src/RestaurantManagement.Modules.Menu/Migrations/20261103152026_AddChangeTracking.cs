using System;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantManagement.Shared.Data.Changes;

#nullable disable

namespace RestaurantManagement.Modules.Menu.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeTracking : Migration
    {
        private static readonly string[] StampedTables = { "menus", "menu_categories", "location_menus" };

        // A detached or deleted link reports the link and its menu at the location they left.
        private const string LinkTombstoneTrigger = """
            CREATE OR REPLACE FUNCTION menu_location_menus_tombstone() RETURNS trigger AS $$
            BEGIN
                INSERT INTO menu_tombstones (id, organization_id, location_id, entity_type, entity_id, change_stamp, deleted_at)
                VALUES (gen_random_uuid(), OLD.organization_id, OLD.location_id, 'LocationMenu', OLD.id, (pg_current_xact_id()::text)::bigint, now()),
                       (gen_random_uuid(), OLD.organization_id, OLD.location_id, 'Menu', OLD.menu_id, (pg_current_xact_id()::text)::bigint, now());
                RETURN OLD;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER menu_location_menus_tombstone AFTER DELETE ON location_menus
            FOR EACH ROW EXECUTE FUNCTION menu_location_menus_tombstone();
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "change_stamp",
                table: "menus",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "change_stamp",
                table: "menu_categories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "change_stamp",
                table: "location_menus",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "menu_tombstones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_stamp = table.Column<long>(type: "bigint", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_tombstones", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_menus_organization_id_change_stamp",
                table: "menus",
                columns: new[] { "organization_id", "change_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_categories_organization_id_change_stamp",
                table: "menu_categories",
                columns: new[] { "organization_id", "change_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_location_menus_organization_id_change_stamp",
                table: "location_menus",
                columns: new[] { "organization_id", "change_stamp" });

            migrationBuilder.CreateIndex(
                name: "ix_menu_tombstones_organization_id_location_id_entity_type_cha",
                table: "menu_tombstones",
                columns: new[] { "organization_id", "location_id", "entity_type", "change_stamp" });

            // Hand-written triggers (approved at plan Gate 1): EF cannot stamp updates with the transaction ID
            // or see rows removed by the menus -> location_menus / menu_categories cascades.
            foreach (var table in StampedTables)
                migrationBuilder.Sql(ChangeTrackingSql.StampTrigger("menu", table));

            migrationBuilder.Sql(ChangeTrackingSql.TombstoneTrigger("menu", "menu_categories", "menu_tombstones", "MenuCategory", "OLD.location_id"));
            migrationBuilder.Sql(LinkTombstoneTrigger);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS menu_location_menus_tombstone ON location_menus; DROP FUNCTION IF EXISTS menu_location_menus_tombstone();");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS menu_menu_categories_tombstone ON menu_categories; DROP FUNCTION IF EXISTS menu_menu_categories_tombstone();");
            foreach (var table in StampedTables)
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS menu_{table}_stamp ON {table}; DROP FUNCTION IF EXISTS menu_{table}_stamp();");

            migrationBuilder.DropTable(
                name: "menu_tombstones");

            migrationBuilder.DropIndex(
                name: "ix_menus_organization_id_change_stamp",
                table: "menus");

            migrationBuilder.DropIndex(
                name: "ix_menu_categories_organization_id_change_stamp",
                table: "menu_categories");

            migrationBuilder.DropIndex(
                name: "ix_location_menus_organization_id_change_stamp",
                table: "location_menus");

            migrationBuilder.DropColumn(
                name: "change_stamp",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "change_stamp",
                table: "menu_categories");

            migrationBuilder.DropColumn(
                name: "change_stamp",
                table: "location_menus");
        }
    }
}
