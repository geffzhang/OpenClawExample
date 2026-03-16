using System.Diagnostics;
using OpenSandbox;
using OpenSandbox.Config;
using OpenSandbox.Models;

const string server = "http://localhost:8090";
const string image = "ghcr.io/openclaw/openclaw:latest";
const int gatewayPort = 18789;
const int timeoutSeconds = 86400;

var token = Environment.GetEnvironmentVariable("OPENCLAW_GATEWAY_TOKEN") ?? "dummy-token-for-sandbox";

Console.WriteLine($"Creating openclaw sandbox with image={image} on OpenSandbox server {server}...");

await using var sandbox = await Sandbox.CreateAsync(new SandboxCreateOptions
{
    Image = image,
    TimeoutSeconds = timeoutSeconds,
    SkipHealthCheck = true,
    Entrypoint =
    [
        "node dist/index.js gateway --port 18789 --allow-unconfigured --verbose"
    ],
    ConnectionConfig = new ConnectionConfig(new ConnectionConfigOptions { Domain = server }),
    Env = new Dictionary<string, string> { ["OPENCLAW_GATEWAY_TOKEN"] = token },
    Metadata = new Dictionary<string, string> { ["example"] = "openclaw" },
    //NetworkPolicy = new NetworkPolicy { DefaultAction = NetworkRuleAction.Deny, Egress = [ new NetworkRule { Action = NetworkRuleAction.Allow, Target = "pypi.org" } ] }
});

await sandbox.WaitUntilReadyAsync(new WaitUntilReadyOptions
{
    ReadyTimeoutSeconds = timeoutSeconds,
    PollingIntervalMillis = 200,
    HealthCheck = CheckOpenClawAsync
});

var endpoint = await sandbox.GetEndpointAsync(gatewayPort);
Console.WriteLine($"Openclaw started finished. Please refer to {endpoint.EndpointAddress}");

return;

static async Task<bool> CheckOpenClawAsync(Sandbox sandbox)
{
    try
    {
        var endpoint = await sandbox.GetEndpointAsync(gatewayPort);
        var url = $"http://{endpoint.EndpointAddress}";
        var start = Stopwatch.StartNew();

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(1)
        };

        for (var attempt = 0; attempt < 150; attempt++)
        {
            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[check] sandbox ready after {start.Elapsed.TotalSeconds:F1}s");
                    return true;
                }
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[check] failed: {ex.Message}");
        return false;
    }
}