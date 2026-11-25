namespace ServerWatch.Agent;

internal sealed record ProcessStatus(string Name, bool Running);
internal sealed record AgentSnapshot(bool Connected, DateTimeOffset? LastHeartbeatUtc, string Message, IReadOnlyList<ProcessStatus> Processes);

internal sealed class AgentState
{
    private readonly object _gate = new();
    private AgentSnapshot _snapshot = new(false, null, "Starting agent…", Array.Empty<ProcessStatus>());
    public event Action? Changed;

    public AgentSnapshot Snapshot { get { lock (_gate) return _snapshot; } }

    public void Update(bool connected, string message, IReadOnlyList<ProcessStatus>? processes = null)
    {
        lock (_gate)
            _snapshot = new AgentSnapshot(connected, connected ? DateTimeOffset.UtcNow : _snapshot.LastHeartbeatUtc,
                message, processes ?? _snapshot.Processes);
        Changed?.Invoke();
    }
}
