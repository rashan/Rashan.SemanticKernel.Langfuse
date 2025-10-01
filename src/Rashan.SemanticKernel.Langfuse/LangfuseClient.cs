using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class LangfuseClient
{
    private readonly HttpClient _httpClient;
    private readonly string _publicKey;
    private readonly string _secretKey;
    private readonly string _baseUrl;

    public LangfuseClient(string publicKey, string secretKey, string baseUrl = "https://cloud.langfuse.com")
    {
        _publicKey = publicKey;
        _secretKey = secretKey;
        _baseUrl = baseUrl;
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
    }

    public async Task<string> CreateTraceAsync(string name, Dictionary<string, object>? metadata = null)
    {
        var traceId = Guid.NewGuid().ToString();
        var trace = new
        {
            id = traceId,
            name,
            metadata,
            timestamp = DateTime.UtcNow
        };

        await _httpClient.PostAsJsonAsync("/api/public/traces", trace);
        return traceId;
    }

    public async Task CreateGenerationAsync(
        string traceId,
        string name,
        string model,
        string? input = null,
        string? output = null,
        Dictionary<string, object>? metadata = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null)
    {
        var generation = new
        {
            traceId,
            name,
            model,
            input,
            output,
            metadata,
            usage = new
            {
                promptTokens,
                completionTokens,
                totalTokens
            },
            startTime = DateTime.UtcNow,
            endTime = DateTime.UtcNow
        };

        await _httpClient.PostAsJsonAsync("/api/public/generations", generation);
    }

    public async Task CreateSpanAsync(
        string traceId,
        string name,
        Dictionary<string, object>? input = null,
        Dictionary<string, object>? output = null,
        Dictionary<string, object>? metadata = null)
    {
        var span = new
        {
            traceId,
            name,
            input,
            output,
            metadata,
            startTime = DateTime.UtcNow,
            endTime = DateTime.UtcNow
        };

        await _httpClient.PostAsJsonAsync("/api/public/spans", span);
    }

    public async Task UpdateTraceAsync(string traceId, Dictionary<string, object>? metadata = null, string? output = null)
    {
        var update = new
        {
            metadata,
            output
        };

        await _httpClient.PatchAsync($"/api/public/traces/{traceId}", 
            new StringContent(JsonSerializer.Serialize(update), Encoding.UTF8, "application/json"));
    }
}