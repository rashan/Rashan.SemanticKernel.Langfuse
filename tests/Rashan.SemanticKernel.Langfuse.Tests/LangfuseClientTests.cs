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
    public void CreateTraceAsync_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateTraceAsync(null!, (Dictionary<string, object>?)null));
    }

    [Fact]
    public void CreateTraceAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => client.CreateTraceAsync("", (Dictionary<string, object>?)null));
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        // Arrange
        var client = new LangfuseClient(_validOptions);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => client.DisposeAsync().AsTask());
        Assert.Null(exception);
    }
}
