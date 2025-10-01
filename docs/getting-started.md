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

## Basic Usage

```csharp
using Microsoft.SemanticKernel;
using Rashan.SemanticKernel.Langfuse;

// Configure your Langfuse credentials
var langfuseOptions = new LangfuseOptions
{
    PublicKey = "your_public_key",
    SecretKey = "your_secret_key",
    // Optional: Endpoint = "https://your-langfuse-instance.com"
};

// Add Langfuse to your Semantic Kernel
var builder = new KernelBuilder();
builder.WithLangfuseObservability(langfuseOptions);
```

## Configuration

The package supports the following configuration options:

- `PublicKey`: Your Langfuse public key
- `SecretKey`: Your Langfuse secret key
- `Endpoint`: (Optional) Custom Langfuse endpoint URL

## Examples

More detailed examples will be added as the package develops.