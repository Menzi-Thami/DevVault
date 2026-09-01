using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Commands.CreateSnippet;
using DevVault.Domain.Common;
using DevVault.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Application;

public class CreateSnippetHandlerTests
{
    private readonly ISnippetRepository _repository = Substitute.For<ISnippetRepository>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task HandleAsync_PersistsSnippet_AndStampsInjectedClock()
    {
        var handler = new CreateSnippetHandler(_repository, _time, NullLogger<CreateSnippetHandler>.Instance);
        var command = new CreateSnippetCommand("Title", "code", "C#", Guid.NewGuid());

        var result = await handler.HandleAsync(command);

        result.Title.ShouldBe("Title");
        result.CreatedAt.ShouldBe(_time.GetUtcNow().UtcDateTime);   // proves the clock is injected
        await _repository.Received(1).AddAsync(Arg.Any<Snippet>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidInput_ThrowsAndDoesNotSave()
    {
        var handler = new CreateSnippetHandler(_repository, _time, NullLogger<CreateSnippetHandler>.Instance);
        var command = new CreateSnippetCommand("", "code", "C#", Guid.NewGuid());

        await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(command));
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
