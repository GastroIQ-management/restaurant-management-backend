namespace RestaurantManagement.Shared.Data.Changes
{
    /// <summary>
    /// The SQL texts module migrations run to stamp rows and leave tombstones. Every argument is a fixed name written
    /// in a migration, never request input. Names carry the module prefix because modules share one database.
    /// </summary>
    public static class ChangeTrackingSql
    {
        /// <summary>A trigger that sets <c>change_stamp</c> on insert and update of <paramref name="table"/>; names carry the module <paramref name="prefix"/>.</summary>
        public static string StampTrigger(string prefix, string table)
        {
            var name = $"{prefix}_{table}_stamp";
            return $$"""
                CREATE OR REPLACE FUNCTION {{name}}() RETURNS trigger AS $$
                BEGIN
                    NEW.change_stamp := (pg_current_xact_id()::text)::bigint;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER {{name}} BEFORE INSERT OR UPDATE ON {{table}}
                FOR EACH ROW EXECUTE FUNCTION {{name}}();
                """;
        }

        /// <summary>
        /// A trigger that leaves a tombstone when a row of <paramref name="table"/> is deleted (also by a database cascade).
        /// <paramref name="locationExpression"/> is the SQL for the tombstone's location (for example <c>OLD.location_id</c> or <c>NULL</c>);
        /// <paramref name="organizationExpression"/> is the SQL for its organization.
        /// </summary>
        public static string TombstoneTrigger(string prefix, string table, string tombstoneTable, string entityType, string locationExpression, string organizationExpression = "OLD.organization_id")
        {
            var name = $"{prefix}_{table}_tombstone";
            return $$"""
                CREATE OR REPLACE FUNCTION {{name}}() RETURNS trigger AS $$
                BEGIN
                    INSERT INTO {{tombstoneTable}} (id, organization_id, location_id, entity_type, entity_id, change_stamp, deleted_at)
                    VALUES (gen_random_uuid(), {{organizationExpression}}, {{locationExpression}}, '{{entityType}}', OLD.id, (pg_current_xact_id()::text)::bigint, now());
                    RETURN OLD;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER {{name}} AFTER DELETE ON {{table}}
                FOR EACH ROW EXECUTE FUNCTION {{name}}();
                """;
        }
    }
}
