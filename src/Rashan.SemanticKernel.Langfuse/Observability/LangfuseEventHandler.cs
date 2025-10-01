using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Rashan.SemanticKernel.Langfuse.Observability;

/// <summary>
/// Activity listener for tracking Semantic Kernel operations in Langfuse using OpenTelemetry.
/// </summary>
internal sealed class LangfuseEventHandler : IDisposable
{
    private readonly ILangfuseClient _client;
    private readonly ILogger<LangfuseEventHandler>? _logger;
    private readonly ConcurrentDictionary<string, string> _activityTraceMapping = new();
    private readonly ActivityListener _activityListener;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the LangfuseEventHandler class.
    /// </summary>
    /// <param name="client">The Langfuse client instance.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LangfuseEventHandler(ILangfuseClient client, ILogger<LangfuseEventHandler>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;

        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Microsoft.SemanticKernel", StringComparison.OrdinalIgnoreCase),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    /// <summary>
    /// Handles the start of an OpenTelemetry activity.
    /// </summary>
    /// <param name="activity">The activity that started.</param>
    private async void OnActivityStarted(Activity activity)
    {
        if (_disposed || activity == null)
            return;

        try
        {
            var traceId = await CreateTraceIfNeededAsync($"SK Activity: {activity.DisplayName}");
            _activityTraceMapping[activity.Id ?? Guid.NewGuid().ToString()] = traceId;

            var metadata = new Dictionary<string, object>
            {
                { "activity_name", activity.DisplayName },
                { "activity_kind", activity.Kind.ToString() },
                { "activity_id", activity.Id ?? "" }
            };

            // Add tags if available
            foreach (var tag in activity.Tags)
            {
                metadata.TryAdd(tag.Key, tag.Value ?? "");
            }

            await _client.CreateEventAsync(
                traceId: traceId,
                name: $"Activity Started: {activity.DisplayName}",
                startTime: activity.StartTimeUtc,
                endTime: activity.StartTimeUtc,
                level: "INFO",
                metadata: JsonSerializer.SerializeToElement(metadata));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling activity started for {ActivityName}", activity.DisplayName);
        }
    }

    /// <summary>
    /// Handles the completion of an OpenTelemetry activity.
    /// </summary>
    /// <param name="activity">The activity that completed.</param>
    private async void OnActivityStopped(Activity activity)
    {
        if (_disposed || activity == null)
            return;

        try
        {
            var activityId = activity.Id ?? Guid.NewGuid().ToString();
            if (!_activityTraceMapping.TryRemove(activityId, out var traceId))
            {
                _logger?.LogWarning("Could not find trace ID for activity {ActivityId}", activityId);
                return;
            }

            var endTime = activity.StartTimeUtc.Add(activity.Duration);
            var metadata = new Dictionary<string, object>
            {
                { "activity_name", activity.DisplayName },
                { "activity_kind", activity.Kind.ToString() },
                { "activity_id", activityId },
                { "duration_ms", activity.Duration.TotalMilliseconds },
                { "status", activity.Status.ToString() }
            };

            // Add tags if available
            foreach (var tag in activity.Tags)
            {
                metadata.TryAdd(tag.Key, tag.Value ?? "");
            }

            // Add events if available
            if (activity.Events.Any())
            {
                var events = activity.Events.Select(e => new
                {
                    name = e.Name,
                    timestamp = e.Timestamp.ToString("O"),
                    tags = e.Tags.ToDictionary(t => t.Key, t => t.Value)
                }).ToArray();
                metadata.Add("events", events);
            }

            // Determine if this should be a span or generation based on activity name/tags
            var isGeneration = activity.DisplayName.Contains("ChatCompletion") || 
                              activity.DisplayName.Contains("TextGeneration") ||
                              activity.Tags.Any(t => t.Key.Contains("model") || t.Key.Contains("llm"));

            if (isGeneration)
            {
                // Extract model info from tags
                var modelTag = activity.Tags.FirstOrDefault(t => t.Key.Contains("model"));
                var model = !modelTag.Equals(default(KeyValuePair<string, string>)) ? modelTag.Value : "unknown";
                
                var promptTag = activity.Tags.FirstOrDefault(t => t.Key.Contains("prompt") || t.Key.Contains("input"));
                var prompt = !promptTag.Equals(default(KeyValuePair<string, string>)) ? promptTag.Value : "";
                
                var responseTag = activity.Tags.FirstOrDefault(t => t.Key.Contains("response") || t.Key.Contains("output"));
                var response = !responseTag.Equals(default(KeyValuePair<string, string>)) ? responseTag.Value : "";

                await _client.CreateGenerationAsync(
                    traceId: traceId,
                    name: activity.DisplayName,
                    startTime: activity.StartTimeUtc,
                    endTime: endTime,
                    model: model,
                    prompt: prompt,
                    response: response,
                    metadata: JsonSerializer.SerializeToElement(metadata));
            }
            else
            {
                await _client.CreateSpanAsync(
                    traceId: traceId,
                    name: activity.DisplayName,
                    input: JsonSerializer.SerializeToElement(new { activity_tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value) }),
                    output: JsonSerializer.SerializeToElement(new { status = activity.Status.ToString() }),
                    metadata: JsonSerializer.SerializeToElement(metadata),
                    startTime: activity.StartTimeUtc,
                    endTime: endTime);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling activity stopped for {ActivityName}", activity.DisplayName);
            // Ensure cleanup even on error
            var activityId = activity.Id ?? Guid.NewGuid().ToString();
            _activityTraceMapping.TryRemove(activityId, out _);
        }
    }

    private async Task<string> CreateTraceIfNeededAsync(string name)
    {
        return await _client.CreateTraceAsync(name, (JsonElement?)null, cancellationToken: default);
    }

    /// <summary>
    /// Disposes the event handler and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _activityListener.Dispose();
        _activityTraceMapping.Clear();
    }
}
