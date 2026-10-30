using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Modules.Organization.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_idempotency_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: true),
                    response_content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    response_body = table.Column<byte[]>(type: "bytea", nullable: true),
                    response_location = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organization_idempotency_keys_created_at",
                table: "organization_idempotency_keys",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_organization_idempotency_keys_user_id_organization_id_key",
                table: "organization_idempotency_keys",
                columns: new[] { "user_id", "organization_id", "key" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_idempotency_keys");
        }
    }
}
