using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rashan.SemanticKernel.Langfuse.Models;

namespace Rashan.SemanticKernel.Langfuse.Observability;

/// <summary>
/// Client for interacting with the Langfuse API.
/// </summary>
internal sealed class LangfuseClient : ILangfuseClient
{
    private readonly HttpClient _httpClient;
    private readonly LangfuseOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<LangfuseClient>? _logger;
    private const string DefaultEndpoint = "https://cloud.langfuse.com";

    /// <summary>
    /// Initializes a new instance of the LangfuseClient class.
    /// </summary>
    /// <param name="options">Configuration options for Langfuse.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    public LangfuseClient(LangfuseOptions options, ILogger<LangfuseClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.PublicKey);
        ArgumentException.ThrowIfNullOrEmpty(options.SecretKey);

        _options = options;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.Endpoint ?? DefaultEndpoint)
        };

        // Use Basic Authentication instead of custom headers for better security
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.PublicKey}:{options.SecretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger?.LogDebug("LangfuseClient initialized with endpoint: {Endpoint}", options.Endpoint ?? DefaultEndpoint);
    }

    /// <summary>
    /// Creates a new LangfuseClient instance with simple parameters.
    /// </summary>
    /// <param name="publicKey">The Langfuse public key.</param>
    /// <param name="secretKey">The Langfuse secret key.</param>
    /// <param name="baseUrl">Optional base URL for the Langfuse API.</param>
    /// <returns>A new ILangfuseClient instance.</returns>
    public static ILangfuseClient Create(
        string publicKey, 
        string secretKey, 
        string? baseUrl = null)
    {
        return new LangfuseClient(new LangfuseOptions
        {
            PublicKey = publicKey,
            SecretKey = secretKey,
            Endpoint = baseUrl
        });
    }

    /// <summary>
    /// Creates a new LangfuseClient instance with simple parameters and logger support.
    /// </summary>
    /// <param name="publicKey">The Langfuse public key.</param>
    /// <param name="secretKey">The Langfuse secret key.</param>
    /// <param name="logger">Logger for structured logging.</param>
    /// <param name="baseUrl">Optional base URL for the Langfuse API.</param>
    /// <returns>A new ILangfuseClient instance.</returns>
    public static ILangfuseClient Create(
        string publicKey, 
        string secretKey, 
        ILogger<LangfuseClient> logger,
        string? baseUrl = null)
    {
        return new LangfuseClient(new LangfuseOptions
        {
            PublicKey = publicKey,
            SecretKey = secretKey,
            Endpoint = baseUrl
        }, logger);
    }

    /// <inheritdoc/>
    public async Task<string> CreateTraceAsync(
        string name,
        JsonElement? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var traceId = Guid.NewGuid().ToString(); // Pre-generate ID like in the other implementation
        var request = new
        {
            id = traceId,
            name,
            metadata = metadata ?? default,
            timestamp = DateTimeOffset.UtcNow
        };

        _logger?.LogDebug("Creating trace: {TraceId} with name: {TraceName}", traceId, name);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/public/traces", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully created trace: {TraceId}", traceId);
            return traceId;
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to create trace {TraceId} with name {TraceName}. Returning pre-generated ID.", traceId, name);
            return traceId; // Still return the pre-generated ID
        }
    }

    /// <inheritdoc/>
    public async Task<string> CreateTraceAsync(
        string name,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var metadataElement = metadata != null 
            ? JsonSerializer.SerializeToElement(metadata, _jsonOptions)
            : (JsonElement?)null;
            
        return await CreateTraceAsync(name, metadataElement, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CreateGenerationAsync(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string model,
        string prompt,
        string response,
        JsonElement? metadata = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);
        
        var duration = endTime - startTime;
        _logger?.LogDebug("Creating generation: {GenerationName} for trace: {TraceId} with model: {Model}, duration: {DurationMs}ms", 
            name, traceId, model, duration.TotalMilliseconds);
        
        var request = new
        {
            traceId,
            name,
            startTime = startTime.ToUnixTimeMilliseconds(),
            endTime = endTime.ToUnixTimeMilliseconds(),
            model,
            input = prompt, // Renamed to match API
            output = response, // Renamed to match API
            metadata = metadata ?? default,
            usage = new // Added token usage tracking
            {
                promptTokens,
                completionTokens,
                totalTokens
            }
        };

        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/public/generations", request, _jsonOptions, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully created generation: {GenerationName} for trace: {TraceId} with {TotalTokens} tokens", 
                name, traceId, totalTokens);
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to create generation {GenerationName} for trace {TraceId} with model {Model}", 
                name, traceId, model);
        }
    }

    /// <inheritdoc/>
    public async Task CreateGenerationAsync(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string model,
        string prompt,
        string response,
        Dictionary<string, object>? metadata = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null,
        CancellationToken cancellationToken = default)
    {
        var metadataElement = metadata != null 
            ? JsonSerializer.SerializeToElement(metadata, _jsonOptions)
            : (JsonElement?)null;
            
        await CreateGenerationAsync(traceId, name, startTime, endTime, model, prompt, response, 
            metadataElement, promptTokens, completionTokens, totalTokens, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CreateSpanAsync(
        string traceId,
        string name,
        JsonElement? input = null,
        JsonElement? output = null,
        JsonElement? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var now = DateTimeOffset.UtcNow;
        var effectiveStartTime = startTime ?? now;
        var effectiveEndTime = endTime ?? now;
        var duration = effectiveEndTime - effectiveStartTime;
        
        _logger?.LogDebug("Creating span: {SpanName} for trace: {TraceId}, duration: {DurationMs}ms", 
            name, traceId, duration.TotalMilliseconds);
        
        var request = new
        {
            traceId,
            name,
            input = input ?? default,
            output = output ?? default,
            metadata = metadata ?? default,
            startTime = effectiveStartTime.ToUnixTimeMilliseconds(),
            endTime = effectiveEndTime.ToUnixTimeMilliseconds()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/public/spans", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully created span: {SpanName} for trace: {TraceId}", name, traceId);
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to create span {SpanName} for trace {TraceId}", name, traceId);
        }
    }

    /// <inheritdoc/>
    public async Task CreateSpanAsync(
        string traceId,
        string name,
        Dictionary<string, object>? input = null,
        Dictionary<string, object>? output = null,
        Dictionary<string, object>? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken cancellationToken = default)
    {
        var inputElement = input != null 
            ? JsonSerializer.SerializeToElement(input, _jsonOptions)
            : (JsonElement?)null;
            
        var outputElement = output != null 
            ? JsonSerializer.SerializeToElement(output, _jsonOptions)
            : (JsonElement?)null;
            
        var metadataElement = metadata != null 
            ? JsonSerializer.SerializeToElement(metadata, _jsonOptions)
            : (JsonElement?)null;
            
        await CreateSpanAsync(traceId, name, inputElement, outputElement, metadataElement, 
            startTime, endTime, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CreateEventAsync(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string level,
        JsonElement? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(level);

        var duration = endTime - startTime;
        _logger?.LogDebug("Creating event: {EventName} for trace: {TraceId} with level: {Level}, duration: {DurationMs}ms", 
            name, traceId, level, duration.TotalMilliseconds);

        var request = new
        {
            traceId,
            name,
            startTime = startTime.ToUnixTimeMilliseconds(),
            endTime = endTime.ToUnixTimeMilliseconds(),
            level,
            metadata = metadata ?? default
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/public/events", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully created event: {EventName} for trace: {TraceId} with level: {Level}", 
                name, traceId, level);
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to create event {EventName} for trace {TraceId} with level {Level}", 
                name, traceId, level);
        }
    }

    /// <summary>
    /// Updates an existing trace with additional metadata or output.
    /// </summary>
    public async Task UpdateTraceAsync(
        string traceId,
        JsonElement? metadata = null,
        string? output = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);

        _logger?.LogDebug("Updating trace: {TraceId} with metadata: {HasMetadata}, output: {HasOutput}", 
            traceId, metadata.HasValue, !string.IsNullOrEmpty(output));

        var update = new
        {
            metadata,
            output
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(update, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync(
                $"api/public/traces/{traceId}",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully updated trace: {TraceId}", traceId);
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to update trace {TraceId}", traceId);
        }
    }

    /// <inheritdoc/>
    public async Task CreateBatchAsync(
        IEnumerable<object> observations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observations);
        
        var observationList = observations.ToList();
        if (!observationList.Any())
        {
            _logger?.LogDebug("Batch request contains no observations, skipping");
            return;
        }

        _logger?.LogDebug("Creating batch with {ObservationCount} observations", observationList.Count);

        var batchRequest = new
        {
            batch = observationList
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/public/ingestion", batchRequest, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            _logger?.LogInformation("Successfully created batch with {ObservationCount} observations", observationList.Count);
        }
        catch (Exception ex) when (!_options.ThrowOnError)
        {
            _logger?.LogWarning(ex, "Failed to create batch with {ObservationCount} observations", observationList.Count);
        }
    }

    /// <summary>
    /// Creates a batch trace observation for use in batch operations.
    /// </summary>
    /// <param name="id">Trace ID.</param>
    /// <param name="name">Trace name.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="timestamp">Optional timestamp.</param>
    /// <returns>A batch trace observation.</returns>
    public static BatchTrace CreateBatchTrace(
        string id,
        string name,
        JsonElement? metadata = null,
        DateTimeOffset? timestamp = null)
    {
        return BatchTrace.Create(id, name, metadata, timestamp);
    }

    /// <summary>
    /// Creates a batch generation observation for use in batch operations.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Generation name.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <param name="model">Model name.</param>
    /// <param name="prompt">Input prompt.</param>
    /// <param name="response">Output response.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="promptTokens">Prompt tokens.</param>
    /// <param name="completionTokens">Completion tokens.</param>
    /// <param name="totalTokens">Total tokens.</param>
    /// <returns>A batch generation observation.</returns>
    public static BatchGeneration CreateBatchGeneration(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string model,
        string prompt,
        string response,
        JsonElement? metadata = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null)
    {
        return BatchGeneration.Create(traceId, name, startTime, endTime, model, prompt, response,
            metadata, promptTokens, completionTokens, totalTokens);
    }

    /// <summary>
    /// Creates a batch span observation for use in batch operations.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Span name.</param>
    /// <param name="input">Optional input data.</param>
    /// <param name="output">Optional output data.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A batch span observation.</returns>
    public static BatchSpan CreateBatchSpan(
        string traceId,
        string name,
        JsonElement? input = null,
        JsonElement? output = null,
        JsonElement? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        return BatchSpan.Create(traceId, name, input, output, metadata, startTime, endTime);
    }

    /// <summary>
    /// Creates a batch event observation for use in batch operations.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Event name.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <param name="level">Log level.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A batch event observation.</returns>
    public static BatchEvent CreateBatchEvent(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string level,
        JsonElement? metadata = null)
    {
        return BatchEvent.Create(traceId, name, startTime, endTime, level, metadata);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_options.ReleaseClientOnDispose)
        {
            _httpClient.Dispose();
        }
        
        await Task.CompletedTask;
    }
}
