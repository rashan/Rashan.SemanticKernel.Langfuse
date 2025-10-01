using System.Text.Json;
using Xunit;
using Rashan.SemanticKernel.Langfuse.Models;
using Rashan.SemanticKernel.Langfuse.Observability;

namespace Rashan.SemanticKernel.Langfuse.Tests;

public class LangfuseClientTests
{
    private readonly LangfuseOptions _validOptions;

    public LangfuseClientTests()
    {
        _validOptions = new LangfuseOptions
        {
            PublicKey = "test_pk",
            SecretKey = "test_sk"
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Act
        var client = new LangfuseClient(_validOptions);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LangfuseClient(null!));
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("public", "")]
    public void Constructor_WithEmptyKeys_ThrowsArgumentException(string publicKey, string secretKey)
    {
        // Arrange
        var options = new LangfuseOptions
        {
            PublicKey = publicKey,
            SecretKey = secretKey
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LangfuseClient(options));
    }

    [Theory]
    [InlineData(null, "secret")]
    [InlineData("public", null)]
    public void Constructor_WithNullKeys_ThrowsArgumentNullException(string? publicKey, string? secretKey)
    {
        // Arrange
        var options = new LangfuseOptions
        {
            PublicKey = publicKey!,
            SecretKey = secretKey!
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LangfuseClient(options));
    }

    [Fact]
    public async Task CreateTraceAsync_WithValidName_ReturnsTraceId()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);
        var name = "Test Trace";

        // Act
        var traceId = await client.CreateTraceAsync(name, (JsonElement?)null);

        // Assert
        Assert.NotNull(traceId);
        Assert.NotEmpty(traceId);
    }

    [Fact]
    public async Task CreateTraceAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateTraceAsync(null!, (JsonElement?)null));
    }

    [Fact]
    public async Task CreateTraceAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateTraceAsync("", (JsonElement?)null));
    }

    [Fact]
    public async Task CreateGenerationAsync_WithValidInputs_Succeeds()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);
        var traceId = await client.CreateTraceAsync("Test Trace", (JsonElement?)null);

        // Act & Assert
        await client.CreateGenerationAsync(
            traceId: traceId,
            name: "Test Generation",
            startTime: DateTimeOffset.UtcNow.AddSeconds(-1),
            endTime: DateTimeOffset.UtcNow,
            model: "gpt-4",
            prompt: "test prompt",
            response: "test response",
            metadata: (JsonElement?)null);
    }

    [Fact]
    public async Task CreateEventAsync_WithValidInputs_Succeeds()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);
        var traceId = await client.CreateTraceAsync("Test Trace", (JsonElement?)null);

        // Act & Assert
        await client.CreateEventAsync(
            traceId: traceId,
            name: "Test Event",
            startTime: DateTimeOffset.UtcNow.AddSeconds(-1),
            endTime: DateTimeOffset.UtcNow,
            level: "INFO",
            metadata: JsonSerializer.SerializeToElement(new { test = "data" }));
    }
}
