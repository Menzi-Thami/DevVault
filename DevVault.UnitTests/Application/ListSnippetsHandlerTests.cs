using DevVault.Application.Common.Exceptions;
using DevVault.Application.Common.Interfaces;
using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.ListSnippets;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DevVault.UnitTests.Application;

public class ListSnippetsHandlerTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("3f0b6c1e-7a2d-4e59-8c34-91d2b7a6e015");
    private static readonly DateTimeOffset At = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ISnippetRepository _repository = Substitute.For<ISnippetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ListSnippetsHandlerTests() => _currentUser.UserId.Returns(CurrentUserId);

    [Theory]
    [InlineData(null, ListSnippetsQuery.DefaultPageSize)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(37, 37)]
    [InlineData(1000, ListSnippetsQuery.MaxPageSize)]
    public async Task PageSize_IsClampedOnTheServer(int? requested, int expected)
    {
        Returns(rows: 0);

        await CreateHandler().HandleAsync(new ListSnippetsQuery(requested));

        // One extra row is fetched to detect a next page.
        await _repository.Received(1).ListAsync(CurrentUserId, expected + 1, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhenMoreRowsExist_ReturnsAFullPage_AndACursorAtItsLastRow()
    {
        var rows = Returns(rows: 3);

        var page = await CreateHandler().HandleAsync(new ListSnippetsQuery(PageSize: 2));

        page.Items.ShouldBe(rows.Take(2));
        page.NextCursor.ShouldBe(new SnippetCursor(rows[1].CreatedAt, rows[1].Id).Encode());
    }

    [Fact]
    public async Task OnTheLastPage_NextCursorIsNull()
    {
        Returns(rows: 2);

        var page = await CreateHandler().HandleAsync(new ListSnippetsQuery(PageSize: 2));

        page.Items.Count.ShouldBe(2);
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task ACursor_IsPassedThroughDecoded()
    {
        var cursor = new SnippetCursor(At, Guid.NewGuid());
        Returns(rows: 0);

        await CreateHandler().HandleAsync(new ListSnippetsQuery(Cursor: cursor.Encode()));

        await _repository.Received(1).ListAsync(
            CurrentUserId, Arg.Any<int>(), cursor, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not-a-cursor!")]
    [InlineData("")]
    [InlineData("AAAA")]
    public async Task AMalformedCursor_IsAValidationError(string cursor) =>
        await Should.ThrowAsync<ValidationException>(
            () => CreateHandler().HandleAsync(new ListSnippetsQuery(Cursor: cursor)));

    private List<SnippetSummaryDto> Returns(int rows)
    {
        var result = Enumerable.Range(0, rows)
            .Select(i => new SnippetSummaryDto(Guid.NewGuid(), $"s{i}", "C#", At.AddMinutes(-i)))
            .ToList();
        _repository.ListAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<SnippetCursor?>(), Arg.Any<CancellationToken>())
            .Returns(result);
        return result;
    }

    private ListSnippetsHandler CreateHandler() =>
        new(_repository, _currentUser, NullLogger<ListSnippetsHandler>.Instance);
}

public class SnippetCursorTests
{
    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        var cursor = new SnippetCursor(new DateTimeOffset(2026, 3, 1, 12, 34, 56, TimeSpan.Zero).AddTicks(1234567), Guid.NewGuid());

        SnippetCursor.TryDecode(cursor.Encode(), out var decoded).ShouldBeTrue();

        decoded.ShouldBe(cursor);
    }

    [Fact]
    public void Encode_IsUrlSafe() =>
        new SnippetCursor(DateTimeOffset.MaxValue, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"))
            .Encode().ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
}
