namespace DevVault.Application.Common.Interfaces;

/// <summary>
/// The authenticated caller, taken from the access token. Owner ids come from here, never from a
/// request body, so a caller cannot act as somebody else.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
