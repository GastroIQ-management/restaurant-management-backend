using RestaurantManagement.Modules.Organization.Entities;
using RestaurantManagement.Modules.Organization.Services;
using Shouldly;

namespace RestaurantManagement.Modules.Organization.Tests
{
    /// <summary>
    /// The business day of a location for a moment, answered to other modules. Cantina da Nonna has Unidade Batel
    /// (Curitiba) and Unidade Toronto; Trattoria do Sol has Unidade Centro. Cantina uses the default 04:00 cutoff; Trattoria uses 05:30.
    /// </summary>
    public class LocationBusinessDayTests
    {
        private const string BatelText = "44444444-dddd-4ddd-8ddd-000000000001";
        private const string CantinaText = "a1a1a1a1-0000-4000-8000-000000000001";
        private const string TrattoriaText = "b2b2b2b2-0000-4000-8000-000000000002";
        private const string CentroText = "44444444-dddd-4ddd-8ddd-000000000003";
        private const string TorontoText = "44444444-dddd-4ddd-8ddd-000000000002";
        private static readonly Guid CantinaId = Guid.Parse("a1a1a1a1-0000-4000-8000-000000000001");
        private static readonly Guid TrattoriaId = Guid.Parse("b2b2b2b2-0000-4000-8000-000000000002");
        private static readonly Guid CentroId = Guid.Parse("44444444-dddd-4ddd-8ddd-000000000003");
        private static readonly DateTime CreationTime = new(2026, 10, 20, 18, 30, 0, DateTimeKind.Utc);

        private static Location Location(Guid id, Guid organizationId, string name, string code, string timeZone) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            Name = name,
            Address = "Rua XV de Novembro, 300",
            City = "Curitiba",
            State = "PR",
            PostalCode = "80020-310",
            Country = "Brasil",
            PhoneNumber = "+55 41 3000-2000",
            Status = LocationStatus.Active,
            TimeZone = timeZone,
            Code = code,
            CreatedAt = CreationTime,
            UpdatedAt = CreationTime
        };

        private static async Task<OrganizationTestDatabase> SeedAsync()
        {
            var database = new OrganizationTestDatabase();
            using var context = database.CreateContext();
            context.Organizations.AddRange(
                new Entities.Organization { Id = CantinaId, Name = "Cantina da Nonna", Currency = Currency.BRL, CreatedAt = CreationTime, UpdatedAt = CreationTime },
                new Entities.Organization { Id = TrattoriaId, Name = "Trattoria do Sol", Currency = Currency.BRL, CreatedAt = CreationTime, UpdatedAt = CreationTime });
            context.OrganizationSettings.AddRange(
                new OrganizationSettings { Id = Guid.NewGuid(), OrganizationId = CantinaId, BusinessDayCutoff = new TimeOnly(4, 0), CreatedAt = CreationTime, UpdatedAt = CreationTime },
                new OrganizationSettings { Id = Guid.NewGuid(), OrganizationId = TrattoriaId, BusinessDayCutoff = new TimeOnly(5, 30), CreatedAt = CreationTime, UpdatedAt = CreationTime });
            context.Locations.AddRange(
                Location(Guid.Parse(BatelText), CantinaId, "Unidade Batel", "BATEL", "America/Sao_Paulo"),
                Location(Guid.Parse(TorontoText), CantinaId, "Unidade Toronto", "TORONTO", "America/Toronto"),
                Location(CentroId, TrattoriaId, "Unidade Centro", "CENTRO", "America/Sao_Paulo"));
            await context.SaveChangesAsync();
            return database;
        }

        // Outcome 3: 23:30 local is already the next day in UTC; 02:00 local is before the 04:00 cutoff
        [Theory]
        [InlineData(CantinaText, BatelText, "2026-11-04T02:30:00Z", "2026-11-03")]
        [InlineData(CantinaText, TorontoText, "2026-11-04T04:30:00Z", "2026-11-03")]
        [InlineData(CantinaText, BatelText, "2026-11-04T05:00:00Z", "2026-11-03")]
        [InlineData(TrattoriaText, CentroText, "2026-11-04T08:00:00Z", "2026-11-03")]
        [Trait("Rule", "OR75")]
        public async Task GetBusinessDayAsync_LocationOfTheTokenOrganization_ReturnsTheLocalBusinessDay(string organizationId, string locationId, string utcMoment, string expectedDay)
        {
            // Arrange
            using var database = await SeedAsync();
            using var context = database.CreateContext(TestTenant.Of(Guid.Parse(organizationId)));
            var sut = new LocationBusinessDay(context, new BusinessDayCalculator());

            // Act
            var day = await sut.GetBusinessDayAsync(Guid.Parse(locationId), DateTime.Parse(utcMoment, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

            // Assert
            day.ShouldBe(DateOnly.Parse(expectedDay), "the business day follows the location's time zone and the organization's cutoff, not universal time");
        }

        // Failure
        [Fact]
        [Trait("Rule", "OR76")]
        public async Task GetBusinessDayAsync_UnknownLocation_ReturnsNull()
        {
            // Arrange
            using var database = await SeedAsync();
            using var context = database.CreateContext(TestTenant.Of(CantinaId));
            var sut = new LocationBusinessDay(context, new BusinessDayCalculator());

            // Act
            var day = await sut.GetBusinessDayAsync(Guid.NewGuid(), new DateTime(2026, 11, 4, 2, 30, 0, DateTimeKind.Utc));

            // Assert
            day.ShouldBeNull("a location that does not exist has no business day");
        }

        // Security: cross-tenant
        [Fact]
        [Trait("Rule", "OR77")]
        public async Task GetBusinessDayAsync_LocationOfAnotherOrganization_ReturnsNullAsIfItDidNotExist()
        {
            // Arrange
            using var database = await SeedAsync();
            using var context = database.CreateContext(TestTenant.Of(CantinaId));
            var sut = new LocationBusinessDay(context, new BusinessDayCalculator());

            // Act
            var day = await sut.GetBusinessDayAsync(CentroId, new DateTime(2026, 11, 4, 2, 30, 0, DateTimeKind.Utc));

            // Assert
            day.ShouldBeNull("another organization's location is answered as not found, never with its business day");
        }
    }
}
