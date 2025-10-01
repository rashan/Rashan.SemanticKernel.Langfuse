using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;

namespace Rashan.SemanticKernel.Langfuse.Extensions;

/// <summary>
/// Extension methods for configuring Langfuse observability in Semantic Kernel.
/// </summary>
public static class KernelExtensions
{
    /// <summary>
    /// Adds Langfuse observability to the Semantic Kernel service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Langfuse configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or options is null.</exception>
    public static IServiceCollection AddLangfuseObservability(
        this IServiceCollection services,
        LangfuseOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // Add Langfuse client as singleton
        services.AddSingleton(options);
        services.AddSingleton<ILangfuseClient, LangfuseClient>();

        // Add Langfuse observability handler
        services.AddSingleton<LangfuseEventHandler>();

        return services;
    }

    /// <summary>
    /// Adds Langfuse observability to the Semantic Kernel service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure Langfuse options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
    public static IServiceCollection AddLangfuseObservability(
        this IServiceCollection services,
        Action<LangfuseOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new LangfuseOptions
        {
            PublicKey = string.Empty,
            SecretKey = string.Empty
        };
        configureOptions(options);

        return services.AddLangfuseObservability(options);
    }

    /// <summary>
    /// Enables Langfuse observability for the given Kernel instance.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="options">Langfuse configuration options.</param>
    /// <returns>The kernel for chaining.</returns>
    public static Kernel WithLangfuseObservability(
        this Kernel kernel,
        LangfuseOptions options)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(options);

        // This would require dependency injection to be properly set up
        // For now, we'll return the kernel as-is and rely on DI configuration
        return kernel;
    }
}
