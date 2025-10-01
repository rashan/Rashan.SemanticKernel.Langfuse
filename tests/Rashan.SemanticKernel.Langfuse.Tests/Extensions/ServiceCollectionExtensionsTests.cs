using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Rashan.SemanticKernel.Langfuse.Extensions;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;
using Xunit;

namespace Rashan.SemanticKernel.Langfuse.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLangfuse_WithValidOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new LangfuseOptions
        {
            PublicKey = "test_pk",
            SecretKey = "test_sk"
        };

        // Act
        services.AddLangfuse(options);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var registeredOptions = serviceProvider.GetService<LangfuseOptions>();
        Assert.NotNull(registeredOptions);
        Assert.Equal(options.PublicKey, registeredOptions.PublicKey);
        Assert.Equal(options.SecretKey, registeredOptions.SecretKey);

        var langfuseClient = serviceProvider.GetService<ILangfuseClient>();
        Assert.NotNull(langfuseClient);
    }

    [Fact]
    public void AddLangfuse_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var options = new LangfuseOptions
        {
            PublicKey = "test_pk",
            SecretKey = "test_sk"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddLangfuse(options));
    }

    [Fact]
    public void AddLangfuse_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddLangfuse((LangfuseOptions)null!));
    }

    [Fact]
    public void AddLangfuse_WithStringParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var publicKey = "test_pk";
        var secretKey = "test_sk";
        var endpoint = "https://test.langfuse.com";

        // Act
        services.AddLangfuse(publicKey, secretKey, endpoint);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<LangfuseOptions>();
        Assert.NotNull(options);
        Assert.Equal(publicKey, options.PublicKey);
        Assert.Equal(secretKey, options.SecretKey);
        Assert.Equal(endpoint, options.Endpoint);

        var langfuseClient = serviceProvider.GetService<ILangfuseClient>();
        Assert.NotNull(langfuseClient);
    }

    [Fact]
    public void AddLangfuse_WithStringParametersWithoutEndpoint_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var publicKey = "test_pk";
        var secretKey = "test_sk";

        // Act
        services.AddLangfuse(publicKey, secretKey);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<LangfuseOptions>();
        Assert.NotNull(options);
        Assert.Equal(publicKey, options.PublicKey);
        Assert.Equal(secretKey, options.SecretKey);
        Assert.Null(options.Endpoint);
    }

    [Fact]
    public void AddLangfuseTracing_WithoutLangfuseServices_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLangfuseTracing();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<TracerProvider>());
    }

    [Fact]
    public void AddLangfuseTracing_WithLangfuseServices_RegistersTracingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Required for OpenTelemetry
        services.AddLangfuse("test_pk", "test_sk");

        // Act
        services.AddLangfuseTracing();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // OpenTelemetry registers TracerProvider as a singleton
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void AddLangfuseTracing_WithCustomConfiguration_CallsConfigureAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLangfuse("test_pk", "test_sk");
        var configureWasCalled = false;

        // Act
        services.AddLangfuseTracing(builder =>
        {
            configureWasCalled = true;
            builder.AddSource("TestSource");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
        Assert.True(configureWasCalled);
    }

    [Fact]
    public void AddLangfuseIntegration_WithOptions_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new LangfuseOptions
        {
            PublicKey = "test_pk",
            SecretKey = "test_sk"
        };

        // Act
        services.AddLangfuseIntegration(options);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var registeredOptions = serviceProvider.GetService<LangfuseOptions>();
        Assert.NotNull(registeredOptions);
        
        var langfuseClient = serviceProvider.GetService<ILangfuseClient>();
        Assert.NotNull(langfuseClient);
        
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void AddLangfuseIntegration_WithStringParameters_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var publicKey = "test_pk";
        var secretKey = "test_sk";
        var endpoint = "https://test.langfuse.com";

        // Act
        services.AddLangfuseIntegration(publicKey, secretKey, endpoint);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<LangfuseOptions>();
        Assert.NotNull(options);
        Assert.Equal(publicKey, options.PublicKey);
        Assert.Equal(secretKey, options.SecretKey);
        Assert.Equal(endpoint, options.Endpoint);
        
        var langfuseClient = serviceProvider.GetService<ILangfuseClient>();
        Assert.NotNull(langfuseClient);
        
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }

    [Fact]
    public void AddLangfuseIntegration_WithCustomTracingConfiguration_CallsConfigureAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configureWasCalled = false;

        // Act
        services.AddLangfuseIntegration("test_pk", "test_sk", null, builder =>
        {
            configureWasCalled = true;
            builder.AddSource("TestSource");
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
        Assert.True(configureWasCalled);
    }

    [Fact]
    public void AddLangfuseIntegration_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var result = services.AddLangfuseIntegration("test_pk", "test_sk");

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddLangfuse_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new LangfuseOptions
        {
            PublicKey = "test_pk",
            SecretKey = "test_sk"
        };

        // Act
        var result = services.AddLangfuse(options);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddLangfuseTracing_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLangfuse("test_pk", "test_sk");

        // Act
        var result = services.AddLangfuseTracing();

        // Assert
        Assert.Same(services, result);
    }
}
