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
    private readonly ISnippetRepository _repository = Substitute.For<ISnippetRepository>();
    private static readonly DateTime At = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetById_WhenFound_ReturnsDto()
    {
        var snippet = Snippet.Create("t", "c", "C#", Guid.NewGuid(), At);
        _repository.GetByIdAsync(snippet.Id, Arg.Any<CancellationToken>()).Returns(snippet);
        var handler = new GetSnippetByIdHandler(_repository, NullLogger<GetSnippetByIdHandler>.Instance);

        var dto = await handler.HandleAsync(snippet.Id);

        dto.Id.ShouldBe(snippet.Id);
    }

    [Fact]
    public async Task GetById_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Snippet?)null);
        var handler = new GetSnippetByIdHandler(_repository, NullLogger<GetSnippetByIdHandler>.Instance);

        await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_MapsAllToDtos()
    {
        var snippets = new[]
        {
            Snippet.Create("a", "c", "C#", Guid.NewGuid(), At),
            Snippet.Create("b", "c", "JS", Guid.NewGuid(), At)
        };
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(snippets);
        var handler = new ListSnippetsHandler(_repository, NullLogger<ListSnippetsHandler>.Instance);

        var result = await handler.HandleAsync();

        result.Count.ShouldBe(2);
        result.Select(r => r.Title).ShouldBe(new[] { "a", "b" });
    }
}
