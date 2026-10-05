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

    [Theory]
    [InlineData(Language.MaxLength, false)]
    [InlineData(Language.MaxLength + 1, true)]
    public void From_Length_IsBoundedAtMax(int length, bool throws)
    {
        var value = new string('x', length);

        if (throws)
            Should.Throw<DomainException>(() => Language.From(value));
        else
            Language.From(value).Value.Length.ShouldBe(length);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Language.From("C#").ShouldBe(Language.From("C#"));   // record value equality
        Language.From("C#").ShouldNotBe(Language.From("F#"));
    }
}
