using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DevVault.IntegrationTests.Fixtures;

public sealed record CapturedLog(string Category, LogLevel Level, string Message, Exception? Exception)
{
    /// <summary>Every key/value from the active logging scopes, e.g. the TraceId the factory adds.</summary>
    public IReadOnlyDictionary<string, object?> Scope { get; init; } = new Dictionary<string, object?>();
}

/// <summary>Records every log entry, with its scopes, so tests can assert what was (and wasn't) logged.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<CapturedLog> _entries = new();
    private IExternalScopeProvider? _scopes;

    public IReadOnlyList<CapturedLog> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose() { }

    private sealed class CapturingLogger(string category, CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            provider._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var scope = new Dictionary<string, object?>();
            provider._scopes?.ForEachScope(static (value, values) =>
            {
                if (value is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    foreach (var (key, item) in pairs)
                        values[key] = item;
                }
            }, scope);

            provider._entries.Enqueue(
                new CapturedLog(category, logLevel, formatter(state, exception), exception) { Scope = scope });
        }
    }
}
