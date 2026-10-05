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
    private static readonly Guid CurrentUserId = Guid.Parse("3f0b6c1e-7a2d-4e59-8c34-91d2b7a6e015");

    private readonly ISnippetRepository _repository = Substitute.For<ISnippetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    public CreateSnippetHandlerTests() => _currentUser.UserId.Returns(CurrentUserId);

    [Fact]
    public async Task HandleAsync_PersistsSnippet_AndStampsInjectedClock()
    {
        var result = await CreateHandler().HandleAsync(new CreateSnippetCommand("Title", "code", "C#"));

        result.Title.ShouldBe("Title");
        result.CreatedAt.ShouldBe(_time.GetUtcNow());   // proves the clock is injected
        await _repository.Received(1).AddAsync(Arg.Any<Snippet>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OwnerIsTheCurrentUser()
    {
        var result = await CreateHandler().HandleAsync(new CreateSnippetCommand("Title", "code", "C#"));

        result.CreatedByUserId.ShouldBe(CurrentUserId);
        await _repository.Received(1).AddAsync(
            Arg.Is<Snippet>(s => s.CreatedByUserId == CurrentUserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidInput_ThrowsAndDoesNotSave()
    {
        await Should.ThrowAsync<DomainException>(
            () => CreateHandler().HandleAsync(new CreateSnippetCommand("", "code", "C#")));
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private CreateSnippetHandler CreateHandler() =>
        new(_repository, _currentUser, _time, NullLogger<CreateSnippetHandler>.Instance);
}
