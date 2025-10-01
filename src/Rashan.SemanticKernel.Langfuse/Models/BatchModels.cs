using System.Text.Json;

namespace Rashan.SemanticKernel.Langfuse.Models;

/// <summary>
/// Base class for batch observations.
/// </summary>
public abstract class BatchObservation
{
    /// <summary>
    /// Gets or sets the type of observation (trace, generation, span, event).
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the observation data.
    /// </summary>
    public required object Body { get; set; }
}

/// <summary>
/// Batch trace observation.
/// </summary>
public sealed class BatchTrace : BatchObservation
{
    /// <summary>
    /// Initializes a new instance of the BatchTrace class.
    /// </summary>
    public BatchTrace()
    {
        Type = "trace-create";
    }

    /// <summary>
    /// Creates a batch trace observation.
    /// </summary>
    /// <param name="id">Trace ID.</param>
    /// <param name="name">Trace name.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="timestamp">Timestamp.</param>
    /// <returns>A batch trace observation.</returns>
    public static BatchTrace Create(string id, string name, JsonElement? metadata = null, DateTimeOffset? timestamp = null)
    {
        return new BatchTrace
        {
            Type = "trace-create",
            Body = new
            {
                id,
                name,
                metadata = metadata ?? default,
                timestamp = timestamp ?? DateTimeOffset.UtcNow
            }
        };
    }
}

/// <summary>
/// Batch generation observation.
/// </summary>
public sealed class BatchGeneration : BatchObservation
{
    /// <summary>
    /// Initializes a new instance of the BatchGeneration class.
    /// </summary>
    public BatchGeneration()
    {
        Type = "generation-create";
    }

    /// <summary>
    /// Creates a batch generation observation.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Generation name.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <param name="model">Model name.</param>
    /// <param name="input">Input prompt.</param>
    /// <param name="output">Output response.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="promptTokens">Prompt tokens.</param>
    /// <param name="completionTokens">Completion tokens.</param>
    /// <param name="totalTokens">Total tokens.</param>
    /// <returns>A batch generation observation.</returns>
    public static BatchGeneration Create(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string model,
        string input,
        string output,
        JsonElement? metadata = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null)
    {
        return new BatchGeneration
        {
            Type = "generation-create",
            Body = new
            {
                traceId,
                name,
                startTime = startTime.ToUnixTimeMilliseconds(),
                endTime = endTime.ToUnixTimeMilliseconds(),
                model,
                input,
                output,
                metadata = metadata ?? default,
                usage = new
                {
                    promptTokens,
                    completionTokens,
                    totalTokens
                }
            }
        };
    }
}

/// <summary>
/// Batch span observation.
/// </summary>
public sealed class BatchSpan : BatchObservation
{
    /// <summary>
    /// Initializes a new instance of the BatchSpan class.
    /// </summary>
    public BatchSpan()
    {
        Type = "span-create";
    }

    /// <summary>
    /// Creates a batch span observation.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Span name.</param>
    /// <param name="input">Optional input data.</param>
    /// <param name="output">Optional output data.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <returns>A batch span observation.</returns>
    public static BatchSpan Create(
        string traceId,
        string name,
        JsonElement? input = null,
        JsonElement? output = null,
        JsonElement? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BatchSpan
        {
            Type = "span-create",
            Body = new
            {
                traceId,
                name,
                input = input ?? default,
                output = output ?? default,
                metadata = metadata ?? default,
                startTime = (startTime ?? now).ToUnixTimeMilliseconds(),
                endTime = (endTime ?? now).ToUnixTimeMilliseconds()
            }
        };
    }
}

/// <summary>
/// Batch event observation.
/// </summary>
public sealed class BatchEvent : BatchObservation
{
    /// <summary>
    /// Initializes a new instance of the BatchEvent class.
    /// </summary>
    public BatchEvent()
    {
        Type = "event-create";
    }

    /// <summary>
    /// Creates a batch event observation.
    /// </summary>
    /// <param name="traceId">Trace ID.</param>
    /// <param name="name">Event name.</param>
    /// <param name="startTime">Start time.</param>
    /// <param name="endTime">End time.</param>
    /// <param name="level">Log level.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <returns>A batch event observation.</returns>
    public static BatchEvent Create(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string level,
        JsonElement? metadata = null)
    {
        return new BatchEvent
        {
            Type = "event-create",
            Body = new
            {
                traceId,
                name,
                startTime = startTime.ToUnixTimeMilliseconds(),
                endTime = endTime.ToUnixTimeMilliseconds(),
                level,
                metadata = metadata ?? default
            }
        };
    }
}
