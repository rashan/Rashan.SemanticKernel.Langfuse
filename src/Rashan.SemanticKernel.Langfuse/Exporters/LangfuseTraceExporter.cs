using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rashan.SemanticKernel.Langfuse.Observability;

namespace Rashan.SemanticKernel.Langfuse.Exporters;

/// <summary>
/// OpenTelemetry trace exporter for Langfuse.
/// Exports Activities (traces) from SemanticKernel to Langfuse as traces, spans, and generations.
/// </summary>
public sealed class LangfuseTraceExporter : BaseExporter<Activity>
{
    private readonly ILangfuseClient _langfuseClient;
    private readonly ILogger<LangfuseTraceExporter>? _logger;
    private readonly Dictionary<string, string> _traceIdMapping = new();

    /// <summary>
    /// Initializes a new instance of the LangfuseTraceExporter class.
    /// </summary>
    /// <param name="langfuseClient">The Langfuse client for API communication.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public LangfuseTraceExporter(ILangfuseClient langfuseClient, ILogger<LangfuseTraceExporter>? logger = null)
    {
        _langfuseClient = langfuseClient ?? throw new ArgumentNullException(nameof(langfuseClient));
        _logger = logger;
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        try
        {
            var activities = new List<Activity>();
            foreach (var activity in batch)
            {
                activities.Add(activity);
            }
            
            _logger?.LogDebug("Exporting {ActivityCount} activities to Langfuse", activities.Count);

            // Process activities in parallel for better performance
            var tasks = activities.Select(ProcessActivityAsync);
            Task.WaitAll(tasks.ToArray());

            _logger?.LogInformation("Successfully exported {ActivityCount} activities to Langfuse", activities.Count);
            return ExportResult.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export activities to Langfuse");
            return ExportResult.Failure;
        }
    }

    /// <summary>
    /// Processes a single activity and sends it to Langfuse.
    /// </summary>
    /// <param name="activity">The activity to process.</param>
    private async Task ProcessActivityAsync(Activity activity)
    {
        try
        {
            // Skip activities that are not from SemanticKernel
            if (!IsSemanticKernelActivity(activity))
            {
                return;
            }

            var traceId = await GetOrCreateTraceIdAsync(activity);
            
            // Determine if this should be a generation or span based on activity type
            if (IsGenerationActivity(activity))
            {
                await CreateGenerationAsync(activity, traceId);
            }
            else
            {
                await CreateSpanAsync(activity, traceId);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to process activity {ActivityName} ({ActivityId})", 
                activity.DisplayName, activity.Id);
        }
    }

    /// <summary>
    /// Determines if an activity is from SemanticKernel.
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if the activity is from SemanticKernel.</returns>
    private static bool IsSemanticKernelActivity(Activity activity)
    {
        return activity.Source?.Name?.StartsWith("Microsoft.SemanticKernel", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines if an activity should be exported as a generation (LLM call) rather than a span.
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if this should be a generation.</returns>
    private static bool IsGenerationActivity(Activity activity)
    {
        if (activity?.DisplayName == null)
            return false;
            
        var displayName = activity.DisplayName.ToLowerInvariant();
        
        return displayName.Contains("chatcompletion") ||
               displayName.Contains("textgeneration") ||
               displayName.Contains("completion") ||
               activity.Tags.Any(tag => 
                   tag.Key.Contains("model", StringComparison.OrdinalIgnoreCase) ||
                   tag.Key.Contains("llm", StringComparison.OrdinalIgnoreCase) ||
                   tag.Key.Equals("gen_ai.operation.name", StringComparison.OrdinalIgnoreCase)
               );
    }

    /// <summary>
    /// Gets or creates a Langfuse trace ID for the given activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The Langfuse trace ID.</returns>
    private async Task<string> GetOrCreateTraceIdAsync(Activity activity)
    {
        // Use the activity's TraceId as the key for trace mapping
        var activityTraceId = activity.TraceId.ToString();
        
        if (_traceIdMapping.TryGetValue(activityTraceId, out var existingTraceId))
        {
            return existingTraceId;
        }

        // Create a new trace in Langfuse
        var traceName = GetTraceNameFromActivity(activity);
        var traceId = await _langfuseClient.CreateTraceAsync(traceName, (Dictionary<string, object>?)null);
        
        _traceIdMapping[activityTraceId] = traceId;
        return traceId;
    }

    /// <summary>
    /// Creates a Langfuse generation from an activity.
    /// </summary>
    /// <param name="activity">The activity representing an LLM call.</param>
    /// <param name="traceId">The Langfuse trace ID.</param>
    private async Task CreateGenerationAsync(Activity activity, string traceId)
    {
        var model = ExtractModelName(activity);
        var prompt = ExtractPrompt(activity);
        var response = ExtractResponse(activity);
        var (promptTokens, completionTokens, totalTokens) = ExtractTokenUsage(activity);

        var metadata = ExtractMetadata(activity);

        await _langfuseClient.CreateGenerationAsync(
            traceId: traceId,
            name: activity.DisplayName,
            startTime: activity.StartTimeUtc,
            endTime: activity.StartTimeUtc.Add(activity.Duration),
            model: model,
            prompt: prompt,
            response: response,
            metadata: metadata,
            promptTokens: promptTokens,
            completionTokens: completionTokens,
            totalTokens: totalTokens
        );
    }

    /// <summary>
    /// Creates a Langfuse span from an activity.
    /// </summary>
    /// <param name="activity">The activity representing a span.</param>
    /// <param name="traceId">The Langfuse trace ID.</param>
    private async Task CreateSpanAsync(Activity activity, string traceId)
    {
        var input = ExtractActivityInput(activity);
        var output = ExtractActivityOutput(activity);
        var metadata = ExtractMetadata(activity);

        await _langfuseClient.CreateSpanAsync(
            traceId: traceId,
            name: activity.DisplayName,
            input: input,
            output: output,
            metadata: metadata,
            startTime: activity.StartTimeUtc,
            endTime: activity.StartTimeUtc.Add(activity.Duration)
        );
    }

    /// <summary>
    /// Extracts a meaningful trace name from an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>A trace name.</returns>
    private static string GetTraceNameFromActivity(Activity activity)
    {
        var rootActivity = GetRootActivity(activity);
        return $"SK: {rootActivity?.DisplayName ?? activity.DisplayName}";
    }

    /// <summary>
    /// Gets the root activity from an activity chain.
    /// </summary>
    /// <param name="activity">The current activity.</param>
    /// <returns>The root activity.</returns>
    private static Activity? GetRootActivity(Activity activity)
    {
        var current = activity;
        while (current?.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }

    /// <summary>
    /// Extracts model name from activity tags.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The model name or "unknown" if not found.</returns>
    private static string ExtractModelName(Activity activity)
    {
        return activity.Tags
            .FirstOrDefault(tag => 
                tag.Key.Equals("gen_ai.request.model", StringComparison.OrdinalIgnoreCase) ||
                tag.Key.Contains("model", StringComparison.OrdinalIgnoreCase))
            .Value ?? "unknown";
    }

    /// <summary>
    /// Extracts prompt from activity events or tags.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The prompt or empty string if not found.</returns>
    private static string ExtractPrompt(Activity activity)
    {
        // Check events first
        var promptEvent = activity.Events
            .FirstOrDefault(e => e.Name.Equals("gen_ai.content.prompt", StringComparison.OrdinalIgnoreCase));
        
        if (promptEvent.Name != null)
        {
            var promptTag = promptEvent.Tags
                .FirstOrDefault(tag => tag.Key.Equals("gen_ai.prompt", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(promptTag.Value?.ToString()))
            {
                return promptTag.Value?.ToString() ?? "";
            }
        }

        // Fall back to tags
        return activity.Tags
            .FirstOrDefault(tag => 
                tag.Key.Contains("prompt", StringComparison.OrdinalIgnoreCase) ||
                tag.Key.Contains("input", StringComparison.OrdinalIgnoreCase))
            .Value ?? "";
    }

    /// <summary>
    /// Extracts response from activity events or tags.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The response or empty string if not found.</returns>
    private static string ExtractResponse(Activity activity)
    {
        // Check events first
        var completionEvent = activity.Events
            .FirstOrDefault(e => e.Name.Equals("gen_ai.content.completion", StringComparison.OrdinalIgnoreCase));
        
        if (completionEvent.Name != null)
        {
            var completionTag = completionEvent.Tags
                .FirstOrDefault(tag => tag.Key.Equals("gen_ai.completion", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(completionTag.Value?.ToString()))
            {
                return completionTag.Value?.ToString() ?? "";
            }
        }

        // Fall back to tags
        return activity.Tags
            .FirstOrDefault(tag => 
                tag.Key.Contains("response", StringComparison.OrdinalIgnoreCase) ||
                tag.Key.Contains("output", StringComparison.OrdinalIgnoreCase) ||
                tag.Key.Contains("completion", StringComparison.OrdinalIgnoreCase))
            .Value ?? "";
    }

    /// <summary>
    /// Extracts token usage information from activity tags.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>A tuple of (promptTokens, completionTokens, totalTokens).</returns>
    private static (int? promptTokens, int? completionTokens, int? totalTokens) ExtractTokenUsage(Activity activity)
    {
        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;

        foreach (var tag in activity.Tags)
        {
            if (tag.Key.Equals("gen_ai.response.prompt_tokens", StringComparison.OrdinalIgnoreCase) ||
                tag.Key.Contains("prompt_token", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(tag.Value, out var prompt))
                    promptTokens = prompt;
            }
            else if (tag.Key.Equals("gen_ai.response.completion_tokens", StringComparison.OrdinalIgnoreCase) ||
                     tag.Key.Contains("completion_token", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(tag.Value, out var completion))
                    completionTokens = completion;
            }
            else if (tag.Key.Equals("gen_ai.response.total_tokens", StringComparison.OrdinalIgnoreCase) ||
                     tag.Key.Contains("total_token", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(tag.Value, out var total))
                    totalTokens = total;
            }
        }

        return (promptTokens, completionTokens, totalTokens);
    }

    /// <summary>
    /// Extracts input data from activity for spans.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>Input data dictionary.</returns>
    private static Dictionary<string, object> ExtractActivityInput(Activity activity)
    {
        var input = new Dictionary<string, object>
        {
            { "activity_name", activity.DisplayName },
            { "activity_id", activity.Id ?? "" },
            { "source", activity.Source?.Name ?? "" }
        };

        // Add relevant tags as input
        foreach (var tag in activity.Tags.Where(t => IsInputTag(t.Key)))
        {
            input.TryAdd(tag.Key, tag.Value ?? "");
        }

        return input;
    }

    /// <summary>
    /// Extracts output data from activity for spans.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>Output data dictionary.</returns>
    private static Dictionary<string, object> ExtractActivityOutput(Activity activity)
    {
        var output = new Dictionary<string, object>
        {
            { "status", activity.Status.ToString() },
            { "duration_ms", activity.Duration.TotalMilliseconds }
        };

        // Add relevant tags as output
        foreach (var tag in activity.Tags.Where(t => IsOutputTag(t.Key)))
        {
            output.TryAdd(tag.Key, tag.Value ?? "");
        }

        return output;
    }

    /// <summary>
    /// Extracts metadata from activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>Metadata dictionary.</returns>
    private static Dictionary<string, object> ExtractMetadata(Activity activity)
    {
        var metadata = new Dictionary<string, object>
        {
            { "activity_id", activity.Id ?? "" },
            { "trace_id", activity.TraceId.ToString() },
            { "span_id", activity.SpanId.ToString() },
            { "source", activity.Source?.Name ?? "" },
            { "duration_ms", activity.Duration.TotalMilliseconds },
            { "status", activity.Status.ToString() }
        };

        // Add all tags that aren't already captured elsewhere
        foreach (var tag in activity.Tags.Where(t => !IsInputTag(t.Key) && !IsOutputTag(t.Key) && !IsTokenTag(t.Key)))
        {
            metadata.TryAdd(tag.Key, tag.Value ?? "");
        }

        // Add events information
        if (activity.Events.Any())
        {
            metadata.Add("events_count", activity.Events.Count());
            metadata.Add("events", activity.Events.Select(e => new
            {
                name = e.Name,
                timestamp = e.Timestamp.ToString("O"),
                tags = e.Tags.ToDictionary(t => t.Key, t => t.Value)
            }).ToArray());
        }

        return metadata;
    }

    /// <summary>
    /// Determines if a tag key represents input data.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <returns>True if it's an input tag.</returns>
    private static bool IsInputTag(string key)
    {
        return key.Contains("input", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("prompt", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("request", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a tag key represents output data.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <returns>True if it's an output tag.</returns>
    private static bool IsOutputTag(string key)
    {
        return key.Contains("output", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("response", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("completion", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("result", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a tag key represents token usage data.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <returns>True if it's a token tag.</returns>
    private static bool IsTokenTag(string key)
    {
        return key.Contains("token", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _traceIdMapping.Clear();
        }
        base.Dispose(disposing);
    }
}
