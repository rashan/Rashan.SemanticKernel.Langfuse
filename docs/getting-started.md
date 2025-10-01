# Getting Started with Rashan.SemanticKernel.Langfuse

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- A Langfuse account and API credentials

## Installation

You can install the package via NuGet Package Manager:

```bash
dotnet add package Rashan.SemanticKernel.Langfuse
```

## Overview

This package provides two main approaches for integrating Semantic Kernel with Langfuse:

1. **OpenTelemetry Integration (Recommended)** - Modern, industry-standard approach
2. **Filter-based Integration (Legacy)** - Simple filter implementation

## Recommended: OpenTelemetry Integration

The OpenTelemetry approach is the recommended method as it follows modern observability standards and provides automatic instrumentation.

### Setup with Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Rashan.SemanticKernel.Langfuse.Extensions;

var builder = Host.CreateApplicationBuilder();

// Add Langfuse integration with OpenTelemetry
builder.Services.AddLangfuseIntegration(
    publicKey: "your_public_key",
    secretKey: "your_secret_key"
    // endpoint: "https://your-langfuse-instance.com" // Optional for self-hosted
);

// Add Semantic Kernel
builder.Services.AddKernel();

var host = builder.Build();

// Get the kernel from DI
var kernel = host.Services.GetRequiredService<Kernel>();

// All Semantic Kernel operations will now be automatically traced to Langfuse
var result = await kernel.InvokePromptAsync("What is the capital of France?");
```

### Setup without Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using Rashan.SemanticKernel.Langfuse.Exporters;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;

// Configure Langfuse
var langfuseOptions = new LangfuseOptions
{
    PublicKey = "your_public_key",
    SecretKey = "your_secret_key",
    Endpoint = "https://your-langfuse-instance.com" // Optional
};

// Create Langfuse client
var langfuseClient = new LangfuseClient(langfuseOptions);

// Configure OpenTelemetry with Langfuse exporter
using var tracerProvider = TracerProviderBuilder.Create()
    .AddSource("Microsoft.SemanticKernel*")
    .AddProcessor(new BatchActivityExportProcessor(
        new LangfuseTraceExporter(langfuseClient)))
    .Build();

// Create and use Semantic Kernel normally
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4", "your-api-key")
    .Build();

var result = await kernel.InvokePromptAsync("What is the capital of France?");
```

## Legacy: Filter-based Integration

The filter-based approach provides a simpler integration but requires manual trace management.

```csharp
using Microsoft.SemanticKernel;
using Rashan.SemanticKernel.Langfuse;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;

// Configure Langfuse
var langfuseOptions = new LangfuseOptions
{
    PublicKey = "your_public_key",
    SecretKey = "your_secret_key",
    Endpoint = "https://your-langfuse-instance.com" // Optional
};

// Create Langfuse client
var langfuseClient = new LangfuseClient(langfuseOptions);

// Create the filter
var langfuseFilter = new LangfuseSemanticKernelFilter(langfuseClient);

// Add to Kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4", "your-api-key")
    .Build();

// Add filters manually
kernel.PromptRenderFilters.Add(langfuseFilter);
kernel.FunctionInvocationFilters.Add(langfuseFilter);

var result = await kernel.InvokePromptAsync("What is the capital of France?");
```

## Configuration Options

### LangfuseOptions

- **PublicKey**: Your Langfuse public key (required)
- **SecretKey**: Your Langfuse secret key (required)  
- **Endpoint**: Custom Langfuse endpoint URL (optional, defaults to Langfuse cloud)
- **ReleaseClientOnDispose**: Whether to dispose the HTTP client when the Langfuse client is disposed (default: true)
- **ThrowOnError**: Whether to throw exceptions on API errors (default: false)

## Advanced Configuration

### Custom OpenTelemetry Configuration

```csharp
builder.Services.AddLangfuseIntegration(
    publicKey: "your_public_key",
    secretKey: "your_secret_key",
    configureTracing: tracingBuilder =>
    {
        // Add additional trace sources
        tracingBuilder.AddSource("MyCustomSource");
        
        // Add other exporters
        tracingBuilder.AddConsoleExporter();
        
        // Configure sampling
        tracingBuilder.SetSampler(new AlwaysOnSampler());
    }
);
```

### Separate Service Registration

For more control, you can register services separately:

```csharp
// Register Langfuse services first
builder.Services.AddLangfuse(new LangfuseOptions
{
    PublicKey = "your_public_key",
    SecretKey = "your_secret_key"
});

// Then add tracing
builder.Services.AddLangfuseTracing(tracingBuilder =>
{
    // Custom configuration
});
```

## Benefits of OpenTelemetry Approach

1. **Industry Standard**: Uses OpenTelemetry, the CNCF standard for observability
2. **Automatic Instrumentation**: Captures all Semantic Kernel activities automatically
3. **Rich Context**: Preserves full trace context and relationships
4. **Performance**: Uses efficient batch processing and async operations
5. **Extensibility**: Easy to add additional exporters and processors
6. **Structured Data**: Better handling of prompts, responses, and metadata
7. **Token Usage**: Automatic extraction of token usage information

## What Gets Tracked

The integration automatically captures:

- **Traces**: High-level operations and their relationships
- **Spans**: Individual steps within operations (function calls, prompt rendering)
- **Generations**: LLM completions with prompts, responses, and token usage
- **Metadata**: Duration, model information, error details
- **Context**: Full trace relationships and hierarchies

## Examples

For more detailed examples and use cases, see the examples directory in the repository.

## Troubleshooting

### Common Issues

1. **Missing API Keys**: Ensure your Langfuse public and secret keys are correctly configured
2. **Network Issues**: Check connectivity to your Langfuse instance
3. **Missing Data**: Verify that OpenTelemetry is properly configured and the correct activity sources are added

### Debugging

Enable debug logging to troubleshoot issues:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
