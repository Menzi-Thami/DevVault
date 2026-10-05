using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace DevVault.Application.Snippets.Queries.ListSnippets;

/// <summary>
/// Keyset position in the (CreatedAt DESC, Id DESC) ordering: the last row of the previous page.
/// Clients see it only as an opaque base64url string.
/// </summary>
public sealed record SnippetCursor(DateTimeOffset CreatedAt, Guid Id)
{
    private const int TicksLength = sizeof(long);
    private const int EncodedLength = TicksLength + 16;

    public string Encode()
    {
        Span<byte> bytes = stackalloc byte[EncodedLength];
        BinaryPrimitives.WriteInt64BigEndian(bytes, CreatedAt.UtcTicks);
        Id.TryWriteBytes(bytes[TicksLength..]);
        return Base64Url.EncodeToString(bytes);
    }

    public static bool TryDecode(string value, [NotNullWhen(true)] out SnippetCursor? cursor)
    {
        cursor = null;
        // Validate first: TryDecodeFromChars throws, rather than returning false, on invalid characters.
        if (!Base64Url.IsValid(value, out var decodedLength) || decodedLength != EncodedLength)
            return false;

        Span<byte> bytes = stackalloc byte[EncodedLength];
        if (!Base64Url.TryDecodeFromChars(value, bytes, out var written) || written != EncodedLength)
            return false;

        var ticks = BinaryPrimitives.ReadInt64BigEndian(bytes);
        if (ticks < DateTimeOffset.MinValue.UtcTicks || ticks > DateTimeOffset.MaxValue.UtcTicks)
            return false;

        cursor = new SnippetCursor(
            new DateTimeOffset(ticks, TimeSpan.Zero),
            new Guid(bytes.Slice(TicksLength, 16)));
        return true;
    }
}
