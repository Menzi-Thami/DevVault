using System.Collections.ObjectModel;

namespace DevVault.IntegrationTests.Fixtures;

/// <summary>
/// Target for the OpenTelemetry in-memory exporters. They add from whichever thread ends the span
/// or log, while the test reads from its own, so every access goes through one lock.
/// </summary>
public sealed class ExportedItems<T> : Collection<T>
{
    private readonly Lock _gate = new();

    public IReadOnlyList<T> Snapshot()
    {
        lock (_gate)
            return [.. Items];
    }

    /// <summary>Exported when the span/log ends, which can be just after the response arrives.</summary>
    public async Task<T> WaitForAsync(Func<T, bool> match)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var found = Snapshot().FirstOrDefault(match);
            if (found is not null)
                return found;
            await Task.Delay(50);
        }

        throw new TimeoutException($"No exported {typeof(T).Name} matched within 5 seconds.");
    }

    protected override void InsertItem(int index, T item)
    {
        lock (_gate)
            base.InsertItem(index, item);
    }

    protected override void ClearItems()
    {
        lock (_gate)
            base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        lock (_gate)
            base.RemoveItem(index);
    }

    protected override void SetItem(int index, T item)
    {
        lock (_gate)
            base.SetItem(index, item);
    }
}
