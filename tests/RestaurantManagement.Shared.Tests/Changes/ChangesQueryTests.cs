using System.ComponentModel.DataAnnotations;
using RestaurantManagement.Shared.Models;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

/// <summary>The page size of a "changes since" read: 1 to 500, 100 when the caller sends none.</summary>
public class ChangesQueryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(501, 1)]
    [InlineData(1, 0)]
    [InlineData(500, 0)]
    [InlineData(null, 0)]
    [Trait("Rule", "SH141")]
    public void Validate_PageSize_OnlyOneToFiveHundredIsAccepted(int? pageSize, int expectedErrors)
    {
        // Arrange
        var query = new ChangesQuery { PageSize = pageSize };
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true);

        // Assert
        results.Count(r => r.MemberNames.Contains(nameof(ChangesQuery.PageSize)))
            .ShouldBe(expectedErrors, "a page holds 1 to 500 entries; anything else is a 400 on pageSize");
    }

    [Fact]
    [Trait("Rule", "SH142")]
    public void EffectivePageSize_NoPageSizeSent_Is100()
    {
        // Arrange
        var query = new ChangesQuery();

        // Act
        var size = query.EffectivePageSize;

        // Assert
        size.ShouldBe(100, "a caller that sends no page size gets pages of 100");
    }
}
