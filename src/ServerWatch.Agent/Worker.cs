using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;

namespace ServerWatch.Agent;

internal sealed class Worker(ILogger<Worker> logger, AgentSettings settings, AgentState state) : BackgroundService
{
    private readonly HttpClient _http = CreateClient(settings);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ServerWatch agent started for {MasterUrl}.", settings.MasterUrl);
        var delay = TimeSpan.FromSeconds(settings.IntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sample = WindowsMetrics.Read();
                var processes = WindowsMetrics.ReadProcesses(settings.Processes);
                using var response = await _http.PostAsJsonAsync("api/v1/heartbeat", new HeartbeatPayload(sample.CpuPercent, sample.MemoryUsedBytes, sample.MemoryTotalBytes, Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0", processes), stoppingToken);
                if (response.IsSuccessStatusCode) { state.Update(true, "Connected to master", processes); delay = TimeSpan.FromSeconds(settings.IntervalSeconds); }
                else if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { state.Update(false, "Master rejected the agent key"); logger.LogError("Master rejected the agent key (HTTP {StatusCode}). Check the protected agent .env file.", (int)response.StatusCode); delay = TimeSpan.FromMinutes(1); }
                else if (response.StatusCode == HttpStatusCode.UnprocessableEntity) { state.Update(false, "Configuration rejected by master"); logger.LogWarning("Master rejected heartbeat configuration (HTTP {StatusCode}); check MONITOR_PROCESSES against the site settings.", (int)response.StatusCode); delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 300)); }
                else { state.Update(false, $"Master returned HTTP {(int)response.StatusCode}"); logger.LogWarning("Heartbeat was rejected with HTTP {StatusCode}; retrying.", (int)response.StatusCode); delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 300)); }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { state.Update(false, $"Reconnecting ({ex.GetType().Name})"); logger.LogWarning("Heartbeat failed ({Reason}); retrying in {DelaySeconds} seconds.", ex.GetType().Name, delay.TotalSeconds); delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 300)); }
            try { await Task.Delay(delay, stoppingToken); } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
    private static HttpClient CreateClient(AgentSettings settings)
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(settings.MasterUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.SiteKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
    public override void Dispose() { _http.Dispose(); base.Dispose(); }
}

internal sealed record HeartbeatPayload(double CpuPercent, ulong MemoryUsedBytes, ulong MemoryTotalBytes, string AgentVersion, IReadOnlyList<ProcessStatus> Processes);
internal sealed record AgentSettings(string MasterUrl, string SiteKey, int IntervalSeconds, IReadOnlyList<string> Processes)
{
    public static AgentSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ".env");
        if (!File.Exists(path)) throw new InvalidOperationException($"Missing agent configuration file: {path}");
        var values = File.ReadLines(path).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')).Select(line => line.Split('=', 2)).Where(pair => pair.Length == 2).ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim().Trim('"', '\''), StringComparer.OrdinalIgnoreCase);
        var master = Required(values, "MONITOR_MASTER_URL");
        if (!Uri.TryCreate(master, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)) throw new InvalidOperationException("MONITOR_MASTER_URL must use HTTPS (HTTP is allowed only for localhost development).");
        var key = Required(values, "MONITOR_SITE_KEY");
        if (!key.StartsWith("sw_", StringComparison.Ordinal) || key.Length < 40) throw new InvalidOperationException("MONITOR_SITE_KEY is missing or invalid.");
        var interval = values.TryGetValue("MONITOR_INTERVAL_SECONDS", out var raw) && int.TryParse(raw, out var seconds) ? seconds : 30;
        if (interval is < 5 or > 3600) throw new InvalidOperationException("MONITOR_INTERVAL_SECONDS must be between 5 and 3600.");
        var processes = values.TryGetValue("MONITOR_PROCESSES", out var configured) && !string.IsNullOrWhiteSpace(configured)
            ? configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(NormalizeProcessName).ToArray()
            : Array.Empty<string>();
        if (processes.Length > 6) throw new InvalidOperationException("MONITOR_PROCESSES can contain at most 6 names.");
        if (processes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != processes.Length) throw new InvalidOperationException("MONITOR_PROCESSES must not contain duplicate names.");
        return new AgentSettings(uri.ToString().TrimEnd('/'), key, interval, processes);
    }
    private static string NormalizeProcessName(string value)
    {
        var name = value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? value[..^4] : value;
        if (name.Length is < 1 or > 64 || name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is ' ' or '_' or '.' or '-' or '(' or ')')))
            throw new InvalidOperationException($"Invalid process name in MONITOR_PROCESSES: {value}");
        return name;
    }
    private static string Required(IReadOnlyDictionary<string, string> values, string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"{name} is required in .env.");
}
