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

This package provides a modern OpenTelemetry-based integration for connecting Semantic Kernel with Langfuse, following industry-standard observability practices with automatic instrumentation.

## OpenTelemetry Integration

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
    publicKey: "pk-...",
    secretKey: "sk-...",
    endpoint: "https://cloud.langfuse.com" // Optional: defaults to Langfuse cloud
);

// Add Semantic Kernel with your preferred AI service
builder.Services.AddKernel()
    .AddOpenAIChatCompletion("gpt-4", "your-openai-api-key");

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

// Configure Langfuse options
var langfuseOptions = new LangfuseOptions
{
    PublicKey = "pk-...",
    SecretKey = "sk-...",
    Endpoint = "https://cloud.langfuse.com" // Optional
};

// Create Langfuse client
var langfuseClient = new LangfuseClient(langfuseOptions);

// Configure OpenTelemetry with Langfuse exporter
using var tracerProvider = TracerProviderBuilder.Create()
    .AddSource("Microsoft.SemanticKernel*")
    .AddProcessor(new BatchActivityExportProcessor(
        new LangfuseTraceExporter(langfuseClient)))
    .Build();

// Create and use Semantic Kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4", "your-openai-api-key")
    .Build();

var result = await kernel.InvokePromptAsync("What is the capital of France?");
Console.WriteLine(result);
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

You can customize the OpenTelemetry configuration to add additional exporters or configure sampling:

```csharp
builder.Services.AddLangfuseIntegration(
    publicKey: "pk-...",
    secretKey: "sk-...",
    configureTracing: tracingBuilder =>
    {
        // Add additional trace sources
        tracingBuilder.AddSource("MyApplication.*");
        
        // Add console exporter for debugging
        tracingBuilder.AddConsoleExporter();
        
        // Configure sampling (optional)
        tracingBuilder.SetSampler(new TraceIdRatioBasedSampler(1.0)); // 100% sampling
    }
);
```

### Separate Service Registration

For advanced scenarios, you can register services separately for more control:

```csharp
// Step 1: Register Langfuse services
builder.Services.AddLangfuse(new LangfuseOptions
{
    PublicKey = "pk-...",
    SecretKey = "sk-...",
    Endpoint = "https://cloud.langfuse.com"
});

// Step 2: Configure OpenTelemetry tracing
builder.Services.AddLangfuseTracing(tracingBuilder =>
{
    // Custom tracing configuration
    tracingBuilder.AddSource("MyApplication.*");
});

// Step 3: Add Semantic Kernel
builder.Services.AddKernel()
    .AddOpenAIChatCompletion("gpt-4", "your-openai-api-key");
```

### Configuration with Options Pattern

```csharp
// Configure via appsettings.json
builder.Services.Configure<LangfuseOptions>(
    builder.Configuration.GetSection("Langfuse"));

// Or configure programmatically
builder.Services.AddLangfuse(options =>
{
    options.PublicKey = "pk-...";
    options.SecretKey = "sk-...";
    options.Endpoint = "https://cloud.langfuse.com";
    options.ThrowOnError = false; // Don't throw exceptions on API errors
    options.ReleaseClientOnDispose = true; // Clean up resources
});
```

# What Gets Tracked

The integration automatically captures:

- **Traces**: High-level operations and their relationships
- **Spans**: Individual steps within operations (function calls, prompt rendering)
- **Generations**: LLM completions with prompts, responses, and token usage
- **Metadata**: Duration, model information, error details
- **Context**: Full trace relationships and hierarchies

## Examples

For hands-on examples and complete working applications, see the [examples directory](../examples/) in the repository:

- **[ChatClient](../examples/ChatClient/)** - Interactive console chat application demonstrating automatic OpenTelemetry integration
- More examples coming soon!

Each example includes:
- Complete source code with documentation
- Step-by-step setup instructions
- Configuration examples for different scenarios
- Troubleshooting guides

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
