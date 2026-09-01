namespace DevVault.Domain.Common;

/// <summary>
/// Raised when a domain invariant is violated. Distinct from framework
/// exceptions so the domain expresses its own failures; mapped to HTTP 400
/// at the API edge by GlobalExceptionMiddleware.
/// </summary>
public class DomainException(string message) : Exception(message);
