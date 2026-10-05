using DevVault.Application.Common.Logging;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Application;

public class LogValueTests
{
    [Theory]
    [InlineData("/api/snippets", "/api/snippets")]
    [InlineData("/api/x\r\nINFO forged entry", "/api/xINFO forged entry")]
    [InlineData("a\nb\rc", "abc")]
    [InlineData(null, "")]
    public void Safe_StripsLineBreaks(string? input, string expected) =>
        LogValue.Safe(input).ShouldBe(expected);
}
