using System.Text.Json;

namespace Rashan.SemanticKernel.Langfuse.Observability;

/// <summary>
/// Interface for Langfuse client operations.
/// </summary>
public interface ILangfuseClient : IAsyncDisposable
{
    /// <summary>
    /// Creates a new trace in Langfuse.
    /// </summary>
    /// <param name="name">Name of the trace.</param>
    /// <param name="metadata">Optional metadata for the trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trace ID.</returns>
    Task<string> CreateTraceAsync(
        string name,
        JsonElement? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new trace in Langfuse.
    /// </summary>
    /// <param name="name">Name of the trace.</param>
    /// <param name="metadata">Optional metadata for the trace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The trace ID.</returns>
    Task<string> CreateTraceAsync(
        string name,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new generation (LLM call) observation.
    /// </summary>
    /// <param name="traceId">The trace ID this generation belongs to.</param>
    /// <param name="name">Name of the generation.</param>
    /// <param name="startTime">Start time of the generation.</param>
    /// <param name="endTime">End time of the generation.</param>
    /// <param name="model">The model used for generation.</param>
    /// <param name="prompt">The prompt used.</param>
    /// <param name="response">The model's response.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="promptTokens">Number of tokens in the prompt.</param>
    /// <param name="completionTokens">Number of tokens in the completion.</param>
    /// <param name="totalTokens">Total number of tokens used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateGenerationAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new generation (LLM call) observation.
    /// </summary>
    /// <param name="traceId">The trace ID this generation belongs to.</param>
    /// <param name="name">Name of the generation.</param>
    /// <param name="startTime">Start time of the generation.</param>
    /// <param name="endTime">End time of the generation.</param>
    /// <param name="model">The model used for generation.</param>
    /// <param name="prompt">The prompt used.</param>
    /// <param name="response">The model's response.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="promptTokens">Number of tokens in the prompt.</param>
    /// <param name="completionTokens">Number of tokens in the completion.</param>
    /// <param name="totalTokens">Total number of tokens used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateGenerationAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new span observation.
    /// </summary>
    /// <param name="traceId">The trace ID this span belongs to.</param>
    /// <param name="name">Name of the span.</param>
    /// <param name="input">Optional input data.</param>
    /// <param name="output">Optional output data.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="startTime">Start time of the span.</param>
    /// <param name="endTime">End time of the span.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateSpanAsync(
        string traceId,
        string name,
        JsonElement? input = null,
        JsonElement? output = null,
        JsonElement? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new span observation.
    /// </summary>
    /// <param name="traceId">The trace ID this span belongs to.</param>
    /// <param name="name">Name of the span.</param>
    /// <param name="input">Optional input data.</param>
    /// <param name="output">Optional output data.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="startTime">Start time of the span.</param>
    /// <param name="endTime">End time of the span.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateSpanAsync(
        string traceId,
        string name,
        Dictionary<string, object>? input = null,
        Dictionary<string, object>? output = null,
        Dictionary<string, object>? metadata = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new event observation.
    /// </summary>
    /// <param name="traceId">The trace ID this event belongs to.</param>
    /// <param name="name">Name of the event.</param>
    /// <param name="startTime">Start time of the event.</param>
    /// <param name="endTime">End time of the event.</param>
    /// <param name="level">Log level of the event.</param>
    /// <param name="metadata">Optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateEventAsync(
        string traceId,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string level,
        JsonElement? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing trace with additional metadata or output.
    /// </summary>
    /// <param name="traceId">The ID of the trace to update.</param>
    /// <param name="metadata">Optional metadata to update.</param>
    /// <param name="output">Optional output to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateTraceAsync(
        string traceId,
        JsonElement? metadata = null,
        string? output = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple observations in a single batch request for improved performance.
    /// </summary>
    /// <param name="observations">Collection of observations to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateBatchAsync(
        IEnumerable<object> observations,
        CancellationToken cancellationToken = default);
}
