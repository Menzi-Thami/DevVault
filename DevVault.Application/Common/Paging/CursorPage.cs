namespace DevVault.Application.Common.Paging;

/// <summary>
/// One page of a keyset-paginated list. Pass <see cref="NextCursor"/> back to get the next page;
/// it is null on the last page.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor);
