using RestaurantManagement.Web.Tests;
using Shouldly;
using static RestaurantManagement.Web.Tests.MenuChangesWorld;

namespace RestaurantManagement.Web.Tests;

/// <summary>
/// The menu categories "changes since" feed on a real PostgreSQL database. A category belongs to the location in its own
/// location field, like the normal per-location read. Cantina da Nonna has categories at Batel and Mercado, Trattoria do Sol at Centro.
/// </summary>
public class MenuCategoryChangesEndpointTests : IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _postgres;

    public MenuCategoryChangesEndpointTests(PostgresDatabaseFixture postgres) => _postgres = postgres;

    private static Guid CategoryId(int n) => Guid.Parse($"66666666-0000-4000-8000-{n:D12}");

    [Fact]
    [Trait("Rule", "M93")]
    public async Task GetCategoryChanges_OneOfThreeCategoriesEdited_ReturnsExactlyItAndNeverOtherLocationsOrOrganizations()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.SeedMenuAsync(TrattoriaId, MenuId(2), "Pizzas");
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId(1), "Massas", BatelId);
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId(2), "Sobremesas", BatelId);
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId(3), "Bebidas", BatelId);
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId(4), "Pratos executivos", MercadoId);
        await factory.SeedCategoryAtAsync(TrattoriaId, MenuId(2), CategoryId(5), "Pizzas salgadas", CentroId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<CategoryRow>(CategoriesRoute);
        await factory.RenameCategoryAsync(CantinaId, CategoryId(2), "Doces da casa");
        await factory.RenameCategoryAsync(CantinaId, CategoryId(4), "Pratos do dia");
        await factory.RenameCategoryAsync(TrattoriaId, CategoryId(5), "Pizzas doces");

        // Act
        var second = await client.ReadChangesAsync<CategoryRow>(CategoriesRoute, first.Cursor);

        // Assert
        first.Changed.Select(c => c.Id).OrderBy(id => id).ShouldBe(new[] { CategoryId(1), CategoryId(2), CategoryId(3) }, ignoreOrder: false, customMessage:
            "the first sync returns only the categories of Batel: none of Mercado's and none of another organization");
        second.Changed.ShouldBe(new[] { new CategoryRow(CategoryId(2), "Doces da casa") }, ignoreOrder: false, customMessage:
            "only the edited Batel category comes back; edits at Mercado and at another organization never do");
    }

    [Fact]
    [Trait("Rule", "M94")]
    public async Task GetCategoryChanges_CategoryDeletedAndMenuWithACategoryDeleted_BothComeBackAsTombstones()
    {
        // Arrange
        await using var factory = await CreateAsync(_postgres);
        await factory.SeedMenuAsync(CantinaId, MenuId(1), "Jantar");
        await factory.SeedMenuAsync(CantinaId, MenuId(2), "Almoço");
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(1), CategoryId(1), "Massas", BatelId);
        await factory.SeedCategoryAtAsync(CantinaId, MenuId(2), CategoryId(2), "Pratos executivos", BatelId);
        var client = factory.AnaAt(BatelId);
        var first = await client.ReadChangesAsync<CategoryRow>(CategoriesRoute);
        await factory.DeleteCategoryAsync(CantinaId, CategoryId(1));
        await factory.DeleteMenuAsync(CantinaId, MenuId(2));

        // Act
        var second = await client.ReadChangesAsync<CategoryRow>(CategoriesRoute, first.Cursor);

        // Assert
        second.Deleted.OrderBy(id => id).ShouldBe(new[] { CategoryId(1), CategoryId(2) }, ignoreOrder: false, customMessage:
            "a deleted category and a category removed with its menu are both reported");
        second.Changed.ShouldBeEmpty("nothing else changed");
    }
}
