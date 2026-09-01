using DevVault.Domain.Common;
using DevVault.Domain.Entities;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Domain;

public class SnippetTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateTime CreatedAt = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

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

    [Fact]
    public void Create_WithEmptyUserId_Throws() =>
        Should.Throw<DomainException>(() => Snippet.Create("t", "c", "C#", Guid.Empty, CreatedAt));
}
