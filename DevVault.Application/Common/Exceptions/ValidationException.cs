namespace DevVault.Application.Common.Exceptions;

/// <summary>
/// Thrown when request input a use case receives is unusable (for example a malformed paging
/// cursor). The API maps it to 400.
/// </summary>
public sealed class ValidationException(string message) : Exception(message);
