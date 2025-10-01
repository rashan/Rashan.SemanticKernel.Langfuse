using Microsoft.SemanticKernel;
using System.Diagnostics;

public class LangfuseSemanticKernelFilter : IPromptRenderFilter, IFunctionInvocationFilter
{
    private readonly LangfuseClient _langfuse;
    private readonly string _traceId;

    public LangfuseSemanticKernelFilter(LangfuseClient langfuse, string? traceId = null)
    {
        _langfuse = langfuse;
        _traceId = traceId ?? Guid.NewGuid().ToString();
    }

    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();

        await _langfuse.CreateSpanAsync(
            _traceId,
            "prompt_render",
            input: new Dictionary<string, object>
            {
                { "function", context.Function.Name },
                { "arguments", context.Arguments.ToDictionary(x => x.Key, x => (object)x.Value.ToString()!) }
            },
            output: new Dictionary<string, object>
            {
                { "rendered_prompt", context.RenderedPrompt ?? "" }
            },
            metadata: new Dictionary<string, object>
            {
                { "duration_ms", stopwatch.ElapsedMilliseconds }
            }
        );
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var functionName = context.Function.Name;
        
        // Extract input
        var input = context.Arguments.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? "");
        
        await next(context);
        
        stopwatch.Stop();

        // Extract output
        var output = context.Result?.ToString() ?? "";
        
        // Extract token usage if available
        int? promptTokens = null;
        int? completionTokens = null;
        int? totalTokens = null;

        if (context.Result?.Metadata?.ContainsKey("Usage") == true)
        {
            var usage = context.Result.Metadata["Usage"];
            if (usage != null)
            {
                var usageType = usage.GetType();
                promptTokens = (int?)usageType.GetProperty("InputTokenCount")?.GetValue(usage);
                completionTokens = (int?)usageType.GetProperty("OutputTokenCount")?.GetValue(usage);
                totalTokens = (int?)usageType.GetProperty("TotalTokenCount")?.GetValue(usage);
            }
        }

        // Get model name
        var modelName = context.Function.Metadata.PluginName ?? "unknown";

        await _langfuse.CreateGenerationAsync(
            _traceId,
            functionName,
            modelName,
            input: string.Join(", ", input.Select(x => $"{x.Key}: {x.Value}")),
            output: output,
            metadata: new Dictionary<string, object>
            {
                { "duration_ms", stopwatch.ElapsedMilliseconds },
                { "plugin", context.Function.Metadata.PluginName ?? "" }
            },
            promptTokens: promptTokens,
            completionTokens: completionTokens,
            totalTokens: totalTokens
        );
    }
}