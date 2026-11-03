using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Modules.Menu.Entities;
using RestaurantManagement.Shared.Auth;
using Currency = RestaurantManagement.Modules.Organization.Entities.Currency;

namespace RestaurantManagement.Web.Tests;

public sealed record MenuRow(Guid Id, string Name, bool IsActive);

public sealed record LinkRow(Guid Id, Guid MenuId, Guid LocationId, bool IsActive);

public sealed record CategoryRow(Guid Id, string Name);

/// <summary>
/// The shared setting of the menu "changes since" tests. Cantina da Nonna (BRL) has Batel and Mercado Municipal, Trattoria do Sol
/// has Centro. Ana owns Cantina, João owns Trattoria, Bia has Menus Read at Batel, Fred only Locations Read at Batel.
/// </summary>
internal static class MenuChangesWorld
{
    public const string AnaText = "11111111-aaaa-4aaa-8aaa-00000000000a";
    public const string BiaText = "22222222-bbbb-4bbb-8bbb-00000000000b";

    public static readonly Guid AnaId = Guid.Parse(AnaText);
    public static readonly Guid BiaId = Guid.Parse(BiaText);
    public static readonly Guid FredId = Guid.Parse("55555555-eeee-4eee-8eee-00000000000e");
    public static readonly Guid JoaoId = Guid.Parse("33333333-cccc-4ccc-8ccc-00000000000c");
    public static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
    public static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
    public static readonly Guid BatelId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000001");
    public static readonly Guid MercadoId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000002");
    public static readonly Guid CentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");

    public const string MenusRoute = "/api/v1/menus/changes";
    public const string LinksRoute = "/api/v1/menus/location-links/changes";
    public const string CategoriesRoute = "/api/v1/menu-categories/changes";

    public static async Task<TenantApiFactory> CreateAsync(PostgresDatabaseFixture postgres)
    {
        var factory = await TenantApiFactory.CreateAsync(postgres);
        await factory.SeedOrganizationAsync(CantinaId, "Cantina da Nonna", Currency.BRL);
        await factory.SeedOrganizationAsync(TrattoriaId, "Trattoria do Sol", Currency.BRL);
        await factory.SeedLocationAsync(CantinaId, BatelId, "Unidade Batel", "BATEL", "America/Sao_Paulo");
        await factory.SeedLocationAsync(CantinaId, MercadoId, "Unidade Mercado Municipal", "MERCADO", "America/Sao_Paulo");
        await factory.SeedLocationAsync(TrattoriaId, CentroId, "Unidade Centro", "CENTRO", "America/Sao_Paulo");
        await factory.SeedUserAsync(AnaId, "ana@cantinadanonna.com.br", "Ana", TenantApiFactory.OwnerOf(CantinaId));
        await factory.SeedUserAsync(JoaoId, "joao@trattoriadosol.com.br", "João", TenantApiFactory.OwnerOf(TrattoriaId));
        await factory.SeedUserAsync(BiaId, "bia@cantinadanonna.com.br", "Bia", TenantApiFactory.MemberOf(CantinaId));
        await factory.SeedUserAsync(FredId, "fred@cantinadanonna.com.br", "Fred", TenantApiFactory.MemberOf(CantinaId));
        await factory.GrantAsync(BiaId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read), (AccessModule.Menus, AccessLevel.Read));
        await factory.GrantAsync(FredId, CantinaId, BatelId, (AccessModule.Locations, AccessLevel.Read));
        return factory;
    }

    public static HttpClient AnaAt(this TenantApiFactory factory, Guid locationId) => factory.ClientFor(AnaId, CantinaId, locationId.ToString());

    public static Task<bool> LinkAsync(this TenantApiFactory factory, Guid organizationId, Guid menuId, Guid locationId, Guid? linkId = null)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            db.LocationMenus.Add(new LocationMenu { Id = linkId ?? Guid.NewGuid(), OrganizationId = organizationId, MenuId = menuId, LocationId = locationId, IsActive = true });
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> DetachAsync(this TenantApiFactory factory, Guid organizationId, Guid menuId, Guid locationId)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            db.LocationMenus.Remove(await db.LocationMenus.SingleAsync(l => l.MenuId == menuId && l.LocationId == locationId));
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> RenameMenuAsync(this TenantApiFactory factory, Guid organizationId, Guid menuId, string name)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            (await db.Menus.SingleAsync(m => m.Id == menuId)).Name = name;
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> DeleteMenuAsync(this TenantApiFactory factory, Guid organizationId, Guid menuId)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            db.Menus.Remove(await db.Menus.SingleAsync(m => m.Id == menuId));
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> SeedCategoryAtAsync(this TenantApiFactory factory, Guid organizationId, Guid menuId, Guid categoryId, string name, Guid locationId)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            db.MenuCategories.Add(new MenuCategory { Id = categoryId, MenuId = menuId, OrganizationId = organizationId, LocationId = locationId, Name = name, SortOrder = 1 });
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> RenameCategoryAsync(this TenantApiFactory factory, Guid organizationId, Guid categoryId, string name)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            (await db.MenuCategories.SingleAsync(c => c.Id == categoryId)).Name = name;
            await db.SaveChangesAsync();
            return true;
        });

    public static Task<bool> DeleteCategoryAsync(this TenantApiFactory factory, Guid organizationId, Guid categoryId)
        => factory.InOrganizationAsync<MenuDbContext, bool>(organizationId, async db =>
        {
            db.MenuCategories.Remove(await db.MenuCategories.SingleAsync(c => c.Id == categoryId));
            await db.SaveChangesAsync();
            return true;
        });

    public static Guid MenuId(int n) => Guid.Parse($"55555555-0000-4000-8000-{n:D12}");
}
