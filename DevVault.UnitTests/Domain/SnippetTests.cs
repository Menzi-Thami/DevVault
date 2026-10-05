using DevVault.Domain.Common;
using DevVault.Domain.Entities;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Domain;

public class SnippetTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidInput_SetsAllFieldsAndGivenTimestamp()
    {
        var snippet = Snippet.Create("  Title  ", "content", " C#  ", User, CreatedAt);

        snippet.Id.ShouldNotBe(Guid.Empty);
        snippet.Title.ShouldBe("Title");            // trimmed
        snippet.Content.ShouldBe("content");
        snippet.Language.Value.ShouldBe("C#");       // value object, trimmed
        snippet.CreatedByUserId.ShouldBe(User);
        snippet.CreatedAt.ShouldBe(CreatedAt);       // deterministic, not DateTime.UtcNow
    }

    [Fact]
    public void Create_StoresTheTimestampAsUtc()
    {
        var local = new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.FromHours(2));

        var snippet = Snippet.Create("t", "c", "C#", User, local);

        snippet.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        snippet.CreatedAt.ShouldBe(local);   // same instant
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankTitle_Throws(string? title) =>
        Should.Throw<DomainException>(() => Snippet.Create(title!, "c", "C#", User, CreatedAt));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankContent_Throws(string content) =>
        Should.Throw<DomainException>(() => Snippet.Create("t", content, "C#", User, CreatedAt));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLanguage_Throws(string language) =>
        Should.Throw<DomainException>(() => Snippet.Create("t", "c", language, User, CreatedAt));

    [Theory]
    [InlineData(Snippet.TitleMaxLength, false)]
    [InlineData(Snippet.TitleMaxLength + 1, true)]
    public void Create_TitleLength_IsBoundedAtMax(int length, bool throws)
    {
        var title = new string('t', length);

        if (throws)
            Should.Throw<DomainException>(() => Snippet.Create(title, "c", "C#", User, CreatedAt));
        else
            Snippet.Create(title, "c", "C#", User, CreatedAt).Title.Length.ShouldBe(length);
    }

    [Fact]
    public void Create_TitleLength_IsMeasuredAfterTrimming() =>
        Snippet.Create($"  {new string('t', Snippet.TitleMaxLength)}  ", "c", "C#", User, CreatedAt)
            .Title.Length.ShouldBe(Snippet.TitleMaxLength);

    [Theory]
    [InlineData(Snippet.ContentMaxLength, false)]
    [InlineData(Snippet.ContentMaxLength + 1, true)]
    public void Create_ContentLength_IsBoundedAtMax(int length, bool throws)
    {
        var content = new string('c', length);

        if (throws)
            Should.Throw<DomainException>(() => Snippet.Create("t", content, "C#", User, CreatedAt));
        else
            Snippet.Create("t", content, "C#", User, CreatedAt).Content.Length.ShouldBe(length);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() =>
        Should.Throw<DomainException>(() => Snippet.Create("t", "c", "C#", Guid.Empty, CreatedAt));
}
