using System.Diagnostics;
using OpenSandbox;
using OpenSandbox.Config;
using OpenSandbox.Models;

const string server = "https://sandbox.lab.csharpkit.com/";
const string image = "ai4c-tcr.tencentcloudcr.com/agentfoundry/king-crab:opensandbox-202603271148";
const int gatewayPort = 18789;
const int timeoutSeconds = 864000;

var token = Environment.GetEnvironmentVariable("OPENCLAW_GATEWAY_TOKEN") ?? "king-crab-demo-token";

Console.WriteLine($"Creating openclaw sandbox with image={image} on OpenSandbox server {server}...");

/// <summary>
/// Default resource limits for sandbox containers.
/// </summary>
var DefaultResourceLimits = new Dictionary<string, string>
{
    ["cpu"] = "0.2",
    ["memory"] = "128M"
};

await using var sandbox = await Sandbox.CreateAsync(new SandboxCreateOptions
{
    ManualCleanup = true,
    Image = image,
    TimeoutSeconds = timeoutSeconds,
    SkipHealthCheck = true,   
    ConnectionConfig = new ConnectionConfig(new ConnectionConfigOptions { Domain = server , ApiKey = "nKFk4hZugnwqeS0ckiY2e2K/RvuN6knnzsQ5vJLqzwc=" }),
    Env = new Dictionary<string, string> { ["OPENCLAW_GATEWAY_TOKEN"] = token },
    Metadata = new Dictionary<string, string> { ["example"] = "openclaw" }, 
    Resource = DefaultResourceLimits,
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