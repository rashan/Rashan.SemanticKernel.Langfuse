# Langfuse SemanticKernel Chat Client Example

This example demonstrates how to integrate SemanticKernel with Langfuse using the modern OpenTelemetry-based approach. It creates an interactive console chat application that automatically traces all AI interactions to Langfuse.


## What Gets Traced

When you use this chat client, the following data is automatically sent to Langfuse:

- **Traces**: Each conversation turn creates a complete trace
- **Generations**: AI model calls with prompts, responses, and token usage
- **Spans**: Individual operations like prompt rendering and function calls
- **Metadata**: Model information, timing, and performance metrics
- **Context**: Full conversation flow and relationships

## Prerequisites

- .NET 8.0 SDK or later
- OpenAI API key
- Langfuse account and API credentials

## Quick Start

### 1. Configure Your Settings

You can configure the application using either `appsettings.json` or environment variables.

**Option A: Using appsettings.json**

Edit the `appsettings.json` file:

```json
{
  "Langfuse": {
    "PublicKey": "pk-lf-your-public-key",
    "SecretKey": "sk-lf-your-secret-key",
    "Endpoint": "https://cloud.langfuse.com"
  },
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "Model": "gpt-4o-mini",
    "Endpoint": "",
    "Provider": "OpenAI"
  }
}
```

**Option B: Using Environment Variables**

```bash
export LANGFUSE_PUBLIC_KEY="pk-lf-your-public-key"
export LANGFUSE_SECRET_KEY="sk-lf-your-secret-key"
export OPENAI_API_KEY="sk-your-openai-api-key"
export OPENAI_ENDPOINT="https://your-custom-endpoint.com"
```

### Custom AI Providers

The example supports multiple AI providers through custom endpoints:

**Standard OpenAI (default):**
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "Model": "gpt-4o-mini",
    "Provider": "OpenAI"
  }
}
```

**Azure OpenAI:**
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-api-key",
    "Model": "gpt-4",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Provider": "Azure"
  }
}
```

**Ollama (local):**
```json
{
  "OpenAI": {
    "ApiKey": "not-used",
    "Model": "llama2",
    "Endpoint": "http://localhost:11434/v1",
    "Provider": "Ollama"
  }
}
```

**Other OpenAI-compatible APIs:**
```json
{
  "OpenAI": {
    "ApiKey": "your-api-key",
    "Model": "your-model",
    "Endpoint": "https://your-api-endpoint.com/v1",
    "Provider": "Custom"
  }
}
```

### 2. Run the Application

```bash
# Navigate to the ChatClient directory
cd examples/ChatClient

# Run the application
dotnet run
```

### 3. Start Chatting

Once the application starts, you'll see:

```
╔══════════════════════════════════════════════════════════════╗
║          Langfuse SemanticKernel Chat Demo                  ║
║          Demonstrating OpenTelemetry Integration            ║
╚══════════════════════════════════════════════════════════════╝

Checking Langfuse connection... ✓ Connected
Checking AI model... ✓ Ready

Chat started! Type your message and press Enter.
Available commands: /help, /clear, /history, /exit

You: Hello! Can you explain what Langfuse does?
AI: Hello! Langfuse is an open-source observability platform specifically designed for Large Language Model (LLM) applications...
```

## Available Commands

- `/help` - Show available commands
- `/clear` - Clear conversation history
- `/history` - Display conversation history
- `/exit` - Exit the application

## Configuration Options

### Langfuse Settings

| Setting | Description | Default | Environment Variable |
|---------|-------------|---------|---------------------|
| `PublicKey` | Your Langfuse public key | Required | `LANGFUSE_PUBLIC_KEY` |
| `SecretKey` | Your Langfuse secret key | Required | `LANGFUSE_SECRET_KEY` |
| `Endpoint` | Langfuse API endpoint | `https://cloud.langfuse.com` | - |

### OpenAI Settings

| Setting | Description | Default | Environment Variable |
|---------|-------------|---------|---------------------|
| `ApiKey` | Your OpenAI API key | Required | `OPENAI_API_KEY` |
| `Model` | OpenAI model to use | `gpt-4o-mini` | - |

### Logging Settings

The application uses structured logging. You can adjust log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Automatic Instrumentation

```csharp
// Add Langfuse integration with OpenTelemetry
builder.Services.AddLangfuseIntegration(
    publicKey: langfusePublicKey,
    secretKey: langfuseSecretKey,
    endpoint: langfuseEndpoint
);

// Add Semantic Kernel with OpenAI
builder.Services.AddKernel()
    .AddOpenAIChatCompletion(modelId: openAiModel, apiKey: openAiApiKey);
```

### Viewing Your Data

After running the chat client:

1. Go to your Langfuse dashboard (e.g., https://cloud.langfuse.com)
2. Navigate to the "Traces" section
3. You'll see traces for each conversation turn with:
   - Complete conversation context
   - Token usage statistics
   - Response times and performance metrics
   - Model information and parameters

## Troubleshooting

### Common Issues

**Authentication Errors**
```
❌ Error: Langfuse PublicKey is required
```
- Ensure your Langfuse credentials are correctly set in `appsettings.json` or environment variables
- Verify your public and secret keys are valid

**OpenAI API Errors**
```
❌ Error: OpenAI ApiKey is required
```
- Check your OpenAI API key in configuration
- Ensure you have sufficient credits in your OpenAI account

**Connection Issues**
```
Checking Langfuse connection... ✗ Failed
```
- Verify your internet connection
- Check if you're behind a corporate firewall
- Ensure the Langfuse endpoint is accessible

### Debug Mode

To enable debug logging, modify `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Rashan.SemanticKernel.Langfuse": "Trace"
    }
  }
}
```

Then run with:
```bash
dotnet run --environment Development
```

## Next Steps

1. **Explore Langfuse**: Check out your traces in the Langfuse dashboard
2. **Customize**: Modify the chat prompts and conversation logic
3. **Extend**: Add more SemanticKernel features like plugins and functions
4. **Monitor**: Use Langfuse to monitor performance and optimize your AI application

## Learn More

- [Langfuse Documentation](https://langfuse.com/docs)
- [SemanticKernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Main Integration Documentation](../../docs/getting-started.md)
