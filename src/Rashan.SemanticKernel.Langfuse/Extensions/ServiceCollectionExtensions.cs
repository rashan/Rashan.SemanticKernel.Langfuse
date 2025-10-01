using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Rashan.SemanticKernel.Langfuse.Exporters;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;

namespace Rashan.SemanticKernel.Langfuse.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure Langfuse integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Langfuse integration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Langfuse configuration options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLangfuse(this IServiceCollection services, LangfuseOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // Register the options
        services.AddSingleton(options);

        // Register the Langfuse client
        services.AddSingleton<ILangfuseClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<LangfuseClient>>();
            return new LangfuseClient(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds Langfuse integration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="publicKey">The Langfuse public key.</param>
    /// <param name="secretKey">The Langfuse secret key.</param>
    /// <param name="endpoint">Optional endpoint URL for Langfuse API (defaults to cloud).</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLangfuse(this IServiceCollection services, string publicKey, string secretKey, string? endpoint = null)
    {
        var options = new LangfuseOptions
        {
            PublicKey = publicKey,
            SecretKey = secretKey,
            Endpoint = endpoint
        };

        return services.AddLangfuse(options);
    }

    /// <summary>
    /// Adds OpenTelemetry tracing with Langfuse exporter to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureTracing">Optional action to configure additional tracing options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method requires that Langfuse services have already been registered using AddLangfuse().
    /// </remarks>
    public static IServiceCollection AddLangfuseTracing(this IServiceCollection services, Action<TracerProviderBuilder>? configureTracing = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource("Microsoft.SemanticKernel*")
                    .AddProcessor(sp =>
                    {
                        var langfuseClient = sp.GetRequiredService<ILangfuseClient>();
                        var logger = sp.GetService<ILogger<LangfuseTraceExporter>>();
                        var exporter = new LangfuseTraceExporter(langfuseClient, logger);
                        return new BatchActivityExportProcessor(exporter);
                    });

                // Allow additional configuration
                configureTracing?.Invoke(builder);
            });

        return services;
    }

    /// <summary>
    /// Adds complete Langfuse integration with OpenTelemetry tracing in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Langfuse configuration options.</param>
    /// <param name="configureTracing">Optional action to configure additional tracing options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLangfuseIntegration(this IServiceCollection services, LangfuseOptions options, Action<TracerProviderBuilder>? configureTracing = null)
    {
        return services
            .AddLangfuse(options)
            .AddLangfuseTracing(configureTracing);
    }

    /// <summary>
    /// Adds complete Langfuse integration with OpenTelemetry tracing in a single call.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="publicKey">The Langfuse public key.</param>
    /// <param name="secretKey">The Langfuse secret key.</param>
    /// <param name="endpoint">Optional endpoint URL for Langfuse API (defaults to cloud).</param>
    /// <param name="configureTracing">Optional action to configure additional tracing options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLangfuseIntegration(this IServiceCollection services, string publicKey, string secretKey, string? endpoint = null, Action<TracerProviderBuilder>? configureTracing = null)
    {
        var options = new LangfuseOptions
        {
            PublicKey = publicKey,
            SecretKey = secretKey,
            Endpoint = endpoint
        };

        return services.AddLangfuseIntegration(options, configureTracing);
    }
}
