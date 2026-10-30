using System.Text;
using RestaurantManagement.Shared.Services.Idempotency;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Idempotency;

/// <summary>"Same request" means same method, same target (path, query and location) and same content.</summary>
public class IdempotencyRequestHashTests
{
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("""{"id":"55555555-0000-4000-8000-000000000001","name":"Jantar"}""");

    [Theory]
    [InlineData("PUT", "/api/v1/menus/create", "44444444-dddd-4ddd-8ddd-000000000001", """{"id":"55555555-0000-4000-8000-000000000001","name":"Jantar"}""")]
    [InlineData("POST", "/api/v1/menus/create?source=tablet", "44444444-dddd-4ddd-8ddd-000000000001", """{"id":"55555555-0000-4000-8000-000000000001","name":"Jantar"}""")]
    [InlineData("POST", "/api/v1/menus/create", "44444444-dddd-4ddd-8ddd-000000000002", """{"id":"55555555-0000-4000-8000-000000000001","name":"Jantar"}""")]
    [InlineData("POST", "/api/v1/menus/create", "44444444-dddd-4ddd-8ddd-000000000001", """{"id":"55555555-0000-4000-8000-000000000001","name":"Almoço"}""")]
    [Trait("Rule", "SH91")]
    public void Compute_AnyPartOfTheRequestChanges_GivesAnotherHash(string method, string pathAndQuery, string location, string body)
    {
        // Arrange
        var original = IdempotencyRequestHash.Compute("POST", "/api/v1/menus/create", "44444444-dddd-4ddd-8ddd-000000000001", Body);

        // Act
        var changed = IdempotencyRequestHash.Compute(method, pathAndQuery, location, Encoding.UTF8.GetBytes(body));

        // Assert
        changed.ShouldNotBe(original, "a different method, path or query, location or body is a different request, so a reused key must be refused");
        original.ShouldBe(IdempotencyRequestHash.Compute("POST", "/api/v1/menus/create", "44444444-dddd-4ddd-8ddd-000000000001", Body),
            "the same request always gives the same hash");
        original.Length.ShouldBe(64, "the hash is a SHA-256 written in hexadecimal");
        original.ShouldNotContain("Jantar", Case.Insensitive, "the body is never part of what is stored");
    }
}
