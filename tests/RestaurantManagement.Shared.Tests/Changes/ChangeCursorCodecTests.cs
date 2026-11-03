using RestaurantManagement.Shared.Services.Changes;
using RestaurantManagement.Shared.Utils.Exceptions;
using Shouldly;

namespace RestaurantManagement.Shared.Tests.Changes;

/// <summary>
/// The opaque change cursor: what it carries, what it refuses (garbage, another feed, organization or location)
/// and how long it lives (14 days, the tombstone retention).
/// </summary>
public class ChangeCursorCodecTests
{
    private const string CantinaText = "a1a1a1a1-0000-4000-8000-000000000001";
    private const string TrattoriaText = "b2b2b2b2-0000-4000-8000-000000000002";
    private const string BatelText = "44444444-dddd-4ddd-8ddd-000000000001";
    private const string MercadoText = "44444444-dddd-4ddd-8ddd-000000000002";

    private static readonly Guid CantinaId = Guid.Parse(CantinaText);
    private static readonly Guid BatelId = Guid.Parse(BatelText);
    private static readonly Guid JantarMenuId = Guid.Parse("55555555-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 15, 0, 0, TimeSpan.Zero);

    private static ChangeCursor CursorIssuedAt(DateTimeOffset issuedAt)
        => new(ChangeFeeds.Menus, CantinaId, BatelId, issuedAt, 4_815_162_342, 4_815_162_400, 4_815_162_399, JantarMenuId);

    private static ChangeCursorCodec CodecAt(DateTimeOffset now) => new(new FixedTimeProvider(now));

    [Fact]
    [Trait("Rule", "SH137")]
    public void Decode_EncodedCursor_KeepsEveryField()
    {
        // Arrange
        var codec = CodecAt(Now);
        var cursor = CursorIssuedAt(Now.AddHours(-2));

        // Act
        var decoded = codec.Decode(codec.Encode(cursor), ChangeFeeds.Menus, CantinaId, BatelId);

        // Assert
        decoded.ShouldBe(cursor, "the cursor keeps feed, organization, location, issue time, both bounds and the paging position");
    }

    [Theory]
    [InlineData("")]
    [InlineData("%%% not a cursor %%%")]
    [InlineData("bm90LWpzb24")]
    [InlineData("e30")]
    [Trait("Rule", "SH138")]
    public void Decode_GarbageOrIncompleteCursor_Throws400CursorInvalid(string text)
    {
        // Arrange
        var codec = CodecAt(Now);

        // Act
        var thrown = Should.Throw<BusinessException>(() => codec.Decode(text, ChangeFeeds.Menus, CantinaId, BatelId));

        // Assert
        thrown.Code.ShouldBe("changes.cursor_invalid", "a cursor the server cannot read is refused with the invalid-cursor code");
        thrown.StatusCode.ShouldBe(400, "an invalid cursor is a bad request");
    }

    [Theory]
    [InlineData(ChangeFeeds.Menus, TrattoriaText, BatelText)]
    [InlineData(ChangeFeeds.Menus, CantinaText, MercadoText)]
    [InlineData(ChangeFeeds.MenuCategories, CantinaText, BatelText)]
    [Trait("Rule", "SH139")]
    public void Decode_CursorBoundToAnotherOrganizationLocationOrFeed_Throws400CursorInvalid(string feed, string organizationText, string locationText)
    {
        // Arrange
        var codec = CodecAt(Now);
        var text = codec.Encode(CursorIssuedAt(Now.AddHours(-2)));

        // Act
        var thrown = Should.Throw<BusinessException>(() => codec.Decode(text, feed, Guid.Parse(organizationText), Guid.Parse(locationText)));

        // Assert
        thrown.Code.ShouldBe("changes.cursor_invalid", "a cursor of Cantina da Nonna's Batel menus is useless for another organization, another location or another feed");
        thrown.StatusCode.ShouldBe(400, "a cursor bound elsewhere is a bad request");
    }

    [Fact]
    [Trait("Rule", "SH140")]
    public void Decode_CursorAroundTheFourteenDayRetention_ThirteenDaysAcceptedFourteenDaysAndOneMinuteGone()
    {
        // Arrange
        var codec = CodecAt(Now);
        var thirteenDaysOld = codec.Encode(CursorIssuedAt(Now.AddDays(-13)));
        var tooOld = codec.Encode(CursorIssuedAt(Now.AddDays(-14).AddMinutes(-1)));

        // Act
        var accepted = codec.Decode(thirteenDaysOld, ChangeFeeds.Menus, CantinaId, BatelId);
        var thrown = Should.Throw<BusinessException>(() => codec.Decode(tooOld, ChangeFeeds.Menus, CantinaId, BatelId));

        // Assert
        accepted.IssuedAt.ShouldBe(Now.AddDays(-13), "a 13 day old cursor is inside the 14 day tombstone retention");
        thrown.Code.ShouldBe("changes.cursor_expired", "a cursor older than 14 days has lost tombstones, so the device must reload in full");
        thrown.StatusCode.ShouldBe(410, "an expired cursor is Gone");
    }
}
