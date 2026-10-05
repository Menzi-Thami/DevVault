using DevVault.API.ErrorHandling;
using DevVault.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevVault.IntegrationTests.ErrorHandling;

/// <summary>The handlers in isolation, for cases that are hard to provoke over HTTP.</summary>
public sealed class ExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetails = Substitute.For<IProblemDetailsService>();
    private readonly CapturingLoggerProvider _logs = new();

    [Fact]
    public async Task Unhandled_ClientAbortedRequest_Is499_NotLoggedAsError_AndWritesNoBody()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = aborted.Token };

        var handled = await CreateUnhandledHandler()
            .TryHandleAsync(context, new OperationCanceledException(aborted.Token), CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(UnhandledExceptionHandler.ClientClosedRequest);
        _logs.Entries.ShouldNotContain(e => e.Level >= LogLevel.Warning);
        await _problemDetails.DidNotReceive().TryWriteAsync(Arg.Any<ProblemDetailsContext>());
    }

    [Fact]
    public async Task Unhandled_CancellationWithoutAbort_IsStillAnError()
    {
        var context = new DefaultHttpContext();

        await CreateUnhandledHandler()
            .TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        _logs.Entries.ShouldContain(e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Known_DoesNotClaimArgumentExceptions()
    {
        var handled = await new KnownExceptionHandler(_problemDetails)
            .TryHandleAsync(new DefaultHttpContext(), new ArgumentException("bug"), CancellationToken.None);

        handled.ShouldBeFalse();
    }

    private UnhandledExceptionHandler CreateUnhandledHandler() =>
        new(_problemDetails, new Logger<UnhandledExceptionHandler>(new LoggerFactory([_logs])));
}
