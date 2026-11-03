using RestaurantManagement.Shared.Data.Changes;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

public class ChangeTrackingSqlTests
{
    [Fact]
    [Trait("Rule", "SH148")]
    public void StampTrigger_MenuModuleTable_NamesThePrefixTheTableAndStampsWithTheTransactionId()
    {
        // Act
        var sql = ChangeTrackingSql.StampTrigger("menu", "menus");

        // Assert
        sql.ShouldContain("menu_", Case.Sensitive, "function and trigger names carry the module prefix: modules share one database");
        sql.ShouldContain("menus", Case.Sensitive, "the trigger is created on the given table");
        sql.ShouldContain("change_stamp", Case.Sensitive, "the trigger writes the stamp column");
        sql.ShouldContain("pg_current_xact_id", Case.Sensitive, "the stamp is the transaction ID of the writing transaction");
        sql.ShouldContain("BEFORE INSERT OR UPDATE", Case.Insensitive, "the stamp is set before the row is stored, on insert and on every update");
        sql.ShouldContain("FOR EACH ROW", Case.Insensitive, "every written row is stamped, not one per statement");
    }
}
