# Rashan.SemanticKernel.Langfuse Examples

This directory contains practical examples demonstrating how to integrate SemanticKernel with Langfuse using the modern OpenTelemetry-based approach.

## Available Examples

### [ChatClient](./ChatClient/) - Interactive Console Chat

A complete console chat application that demonstrates:

- ✅ **OpenTelemetry Integration**: Automatic tracing of all SemanticKernel operations
- ✅ **Dependency Injection**: Modern .NET hosting with full DI support
- ✅ **Configuration Management**: Flexible configuration with JSON files and environment variables
- ✅ **Error Handling**: Robust error handling with user-friendly messages
- ✅ **Conversation Context**: Maintains chat history and context
- ✅ **Command System**: Built-in commands for chat management

**Perfect for**: Learning the integration, testing your setup, and understanding how automatic tracing works.

## Quick Start

Choose an example and follow its README for detailed setup instructions:

```bash
# Navigate to an example
cd examples/ChatClient

# Follow the example's README
cat README.md

# Run the example
dotnet run
```

## Prerequisites

All examples require:

- **.NET 8.0 SDK** or later
- **Langfuse Account**: Get your API keys from [Langfuse](https://langfuse.com)
- **OpenAI API Key**: Required for AI model access

## Getting Langfuse Credentials

1. **Sign up** at [Langfuse](https://langfuse.com)
2. **Create a project** in your dashboard
3. **Get your API keys**:
   - Public Key (starts with `pk-`)
   - Secret Key (starts with `sk-`)

## Common Configuration

All examples support both configuration files and environment variables:

### Using Configuration Files

Edit the example's `appsettings.json`:

```json
{
  "Langfuse": {
    "PublicKey": "pk-lf-your-public-key",
    "SecretKey": "sk-lf-your-secret-key",
    "Endpoint": "https://cloud.langfuse.com"
  },
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "Model": "gpt-4o-mini"
  }
}
```

### Using Environment Variables

```bash
export LANGFUSE_PUBLIC_KEY="pk-lf-your-public-key"
export LANGFUSE_SECRET_KEY="sk-lf-your-secret-key"
export OPENAI_API_KEY="sk-your-openai-api-key"
```

## Viewing Your Data

After running any example:

1. Go to your **Langfuse dashboard**
2. Navigate to the **"Traces"** section
3. Explore your automatically captured data:
   - Complete conversation flows
   - Token usage and costs
   - Performance metrics
   - Model parameters

## Troubleshooting

### Common Issues

**Missing Dependencies**
```bash
# Restore packages if needed
dotnet restore
```

### Getting Help

1. Check the example's specific README
2. Review the [main documentation](../docs/getting-started.md)
3. Enable debug logging in `appsettings.Development.json`
4. Check the [Langfuse documentation](https://langfuse.com/docs)

## Contributing Examples

Have an idea for a new example? Contributions are welcome!

### Example Ideas

- **Function Calling**: Demonstrating SemanticKernel plugins with Langfuse tracing
- **RAG Application**: Retrieval-augmented generation with vector databases
- **Streaming Chat**: Real-time streaming responses with tracing
- **Multi-Model**: Using different AI providers with unified tracing
- **Web API**: ASP.NET Core web API with Langfuse integration
- **Blazor App**: Interactive web application with real-time tracing

### Contributing Guidelines

1. **Follow the Pattern**: Use the same project structure as existing examples
2. **Complete Documentation**: Include a comprehensive README
3. **Error Handling**: Implement robust error handling
4. **Configuration**: Support both JSON and environment variable configuration
5. **Testing**: Ensure the example works end-to-end

## Learn More

- [Getting Started Guide](../docs/getting-started.md)
- [Langfuse Documentation](https://langfuse.com/docs)
- [SemanticKernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

**Happy coding!** 🚀
