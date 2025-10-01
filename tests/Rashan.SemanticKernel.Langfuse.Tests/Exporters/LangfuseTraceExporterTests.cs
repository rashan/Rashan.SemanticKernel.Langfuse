using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry;
using Rashan.SemanticKernel.Langfuse.Exporters;
using Rashan.SemanticKernel.Langfuse.Observability;
using Xunit;

namespace Rashan.SemanticKernel.Langfuse.Tests.Exporters;

public class LangfuseTraceExporterTests : IDisposable
{
    private readonly Mock<ILangfuseClient> _mockLangfuseClient;
    private readonly Mock<ILogger<LangfuseTraceExporter>> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly LangfuseTraceExporter _exporter;

    public LangfuseTraceExporterTests()
    {
        _mockLangfuseClient = new Mock<ILangfuseClient>();
        _mockLogger = new Mock<ILogger<LangfuseTraceExporter>>();
        _activitySource = new ActivitySource("Microsoft.SemanticKernel.Test");
        _exporter = new LangfuseTraceExporter(_mockLangfuseClient.Object, _mockLogger.Object);

        // Setup mock to return trace IDs
        _mockLangfuseClient
            .Setup(x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-trace-id");
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _exporter.Dispose();
    }

    [Fact]
    public void Constructor_WithNullLangfuseClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LangfuseTraceExporter(null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var exporter = new LangfuseTraceExporter(_mockLangfuseClient.Object);

        // Assert
        Assert.NotNull(exporter);
    }

    [Fact]
    public void Export_WithEmptyBatch_ReturnsSuccess()
    {
        // Arrange
        var batch = new Batch<Activity>(Array.Empty<Activity>(), 0);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
    }

    [Fact]
    public void Export_WithNonSemanticKernelActivity_ReturnsSuccess()
    {
        // Arrange
        using var nonSkActivitySource = new ActivitySource("SomeOther.Source");
        
        // Create an activity listener to enable the activity source
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        
        using var activity = nonSkActivitySource.StartActivity("NonSK.Operation");
        
        // Skip test if activity creation failed
        if (activity == null)
        {
            Assert.True(true, "Activity creation failed - skipping test");
            return;
        }
        
        activity.Stop();
        var activities = new[] { activity };
        var batch = new Batch<Activity>(activities, activities.Length);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
        
        // Verify no calls to Langfuse client
        _mockLangfuseClient.Verify(
            x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()), 
            Times.Never);
        
        nonSkActivitySource.Dispose();
    }

    [Fact]
    public void Export_WithSemanticKernelGenerationActivity_CallsCreateGeneration()
    {
        // Arrange
        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.ChatCompletion");
        using var activity = skActivitySource.StartActivity("ChatCompletion");
        
        activity?.SetTag("gen_ai.request.model", "gpt-4");
        activity?.SetTag("gen_ai.operation.name", "chat_completion");
        activity?.Stop();

        var activities = new[] { activity! };
        var batch = new Batch<Activity>(activities, activities.Length);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
        
        // Verify trace creation
        _mockLangfuseClient.Verify(
            x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Verify generation creation
        _mockLangfuseClient.Verify(
            x => x.CreateGenerationAsync(
                It.IsAny<string>(), // traceId
                It.IsAny<string>(), // name
                It.IsAny<DateTimeOffset>(), // startTime
                It.IsAny<DateTimeOffset>(), // endTime
                It.IsAny<string>(), // model
                It.IsAny<string>(), // prompt
                It.IsAny<string>(), // response
                It.IsAny<Dictionary<string, object>?>(), // metadata
                It.IsAny<int?>(), // promptTokens
                It.IsAny<int?>(), // completionTokens
                It.IsAny<int?>(), // totalTokens
                It.IsAny<CancellationToken>()), // cancellationToken
            Times.Once);
        
        skActivitySource.Dispose();
    }

    [Fact]
    public void Export_WithSemanticKernelSpanActivity_CallsCreateSpan()
    {
        // Arrange
        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.Function");
        using var activity = skActivitySource.StartActivity("FunctionInvocation");
        
        activity?.SetTag("function.name", "TestFunction");
        activity?.Stop();

        var activities = new[] { activity! };
        var batch = new Batch<Activity>(activities, activities.Length);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
        
        // Verify trace creation
        _mockLangfuseClient.Verify(
            x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Verify span creation (not generation)
        _mockLangfuseClient.Verify(
            x => x.CreateSpanAsync(
                It.IsAny<string>(), // traceId
                It.IsAny<string>(), // name
                It.IsAny<Dictionary<string, object>?>(), // input
                It.IsAny<Dictionary<string, object>?>(), // output
                It.IsAny<Dictionary<string, object>?>(), // metadata
                It.IsAny<DateTimeOffset?>(), // startTime
                It.IsAny<DateTimeOffset?>(), // endTime
                It.IsAny<CancellationToken>()), // cancellationToken
            Times.Once);
        
        skActivitySource.Dispose();
    }

    [Fact]
    public void Export_WithMultipleActivities_ProcessesAll()
    {
        // Arrange
        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.Test");
        using var activity1 = skActivitySource.StartActivity("Operation1");
        using var activity2 = skActivitySource.StartActivity("Operation2");
        
        activity1?.Stop();
        activity2?.Stop();

        var activities = new[] { activity1!, activity2! };
        var batch = new Batch<Activity>(activities, activities.Length);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        Assert.Equal(ExportResult.Success, result);
        
        // Since both activities share the same TraceId, only one trace should be created
        _mockLangfuseClient.Verify(
            x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // But both spans should be created
        _mockLangfuseClient.Verify(
            x => x.CreateSpanAsync(
                It.IsAny<string>(), // traceId
                It.IsAny<string>(), // name
                It.IsAny<Dictionary<string, object>?>(), // input
                It.IsAny<Dictionary<string, object>?>(), // output
                It.IsAny<Dictionary<string, object>?>(), // metadata
                It.IsAny<DateTimeOffset?>(), // startTime
                It.IsAny<DateTimeOffset?>(), // endTime
                It.IsAny<CancellationToken>()), // cancellationToken
            Times.Exactly(2));
        
        skActivitySource.Dispose();
    }

    [Fact]
    public void Export_WithException_ReturnsSuccess()
    {
        // Arrange
        _mockLangfuseClient
            .Setup(x => x.CreateTraceAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.Test");
        using var activity = skActivitySource.StartActivity("Operation");
        activity?.Stop();

        var activities = new[] { activity! };
        var batch = new Batch<Activity>(activities, activities.Length);

        // Act
        var result = _exporter.Export(in batch);

        // Assert
        // Export succeeds even with individual activity failures because exceptions are caught per activity
        Assert.Equal(ExportResult.Success, result);
        
        skActivitySource.Dispose();
    }

    [Theory]
    [InlineData("ChatCompletion", true)]
    [InlineData("TextGeneration", true)]
    [InlineData("Completion", true)]
    [InlineData("FunctionInvocation", false)]
    [InlineData("PromptRender", false)]
    public void IsGenerationActivity_WithDifferentActivityNames_ReturnsExpectedResult(string activityName, bool expectedIsGeneration)
    {
        // Arrange
        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.Test");
        using var activity = skActivitySource.StartActivity(activityName);
        
        // Use reflection to test the private method
        var method = typeof(LangfuseTraceExporter).GetMethod("IsGenerationActivity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (bool)method!.Invoke(null, new object[] { activity! })!;

        // Assert
        Assert.Equal(expectedIsGeneration, result);
        
        skActivitySource.Dispose();
    }

    [Fact]
    public void IsGenerationActivity_WithModelTags_ReturnsTrue()
    {
        // Arrange
        using var skActivitySource = new ActivitySource("Microsoft.SemanticKernel.Test");
        using var activity = skActivitySource.StartActivity("SomeOperation");
        activity?.SetTag("gen_ai.request.model", "gpt-4");
        
        // Use reflection to test the private method
        var method = typeof(LangfuseTraceExporter).GetMethod("IsGenerationActivity", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (bool)method!.Invoke(null, new object[] { activity! })!;

        // Assert
        Assert.True(result);
        
        skActivitySource.Dispose();
    }
}
