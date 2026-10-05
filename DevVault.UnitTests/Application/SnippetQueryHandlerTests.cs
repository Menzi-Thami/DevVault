using DevVault.Application.Common.Exceptions;
using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Queries.GetSnippetById;
using DevVault.Application.Snippets.Queries.ListSnippets;
using DevVault.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Application;

public class SnippetQueryHandlerTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("3f0b6c1e-7a2d-4e59-8c34-91d2b7a6e015");
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ISnippetRepository _repository = Substitute.For<ISnippetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public SnippetQueryHandlerTests() => _currentUser.UserId.Returns(CurrentUserId);

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var snippet = Snippet.Create("t", "c", "C#", CurrentUserId, At);
        _repository.GetByIdAsync(snippet.Id, CurrentUserId, Arg.Any<CancellationToken>()).Returns(snippet);

        var dto = await CreateGetHandler().HandleAsync(snippet.Id);

        dto.Id.ShouldBe(snippet.Id);
    }

    [Fact]
    public async Task GetById_WhenMissingForThisUser_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Snippet?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateGetHandler().HandleAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_LooksUpOnlyTheCurrentUsersSnippets()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Snippet?)null);

        await Should.ThrowAsync<NotFoundException>(() => CreateGetHandler().HandleAsync(id));

        await _repository.Received(1).GetByIdAsync(id, CurrentUserId, Arg.Any<CancellationToken>());
    }

    private GetSnippetByIdHandler CreateGetHandler() =>
        new(_repository, _currentUser, NullLogger<GetSnippetByIdHandler>.Instance);
}
