using DevVault.Domain.Common;
using DevVault.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Domain;

public class LanguageTests
{
    [Fact]
    public void From_TrimsAndStoresValue()
    {
        var language = Language.From("  Python ");
        language.Value.ShouldBe("Python");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_WithBlank_Throws(string? value) =>
        Should.Throw<DomainException>(() => Language.From(value!));

    [Fact]
    public void Equality_IsByValue()
    {
        Language.From("C#").ShouldBe(Language.From("C#"));   // record value equality
        Language.From("C#").ShouldNotBe(Language.From("F#"));
    }
}
