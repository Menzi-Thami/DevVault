namespace DevVault.Application.Common.Exceptions;

/// <summary>Thrown when a requested aggregate does not exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"{name} with id '{key}' was not found.") { }
}
