namespace Rashan.SemanticKernel.Langfuse.Models;

/// <summary>
/// Configuration options for Langfuse integration.
/// </summary>
public class LangfuseOptions
{
    /// <summary>
    /// Gets or sets the Langfuse public key.
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Gets or sets the Langfuse secret key.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the Langfuse endpoint URL.
    /// Defaults to the public Langfuse API if not specified.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets whether to release the client when the kernel is disposed.
    /// Defaults to true.
    /// </summary>
    public bool ReleaseClientOnDispose { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw exceptions on API errors.
    /// Defaults to false.
    /// </summary>
    public bool ThrowOnError { get; set; } = false;
}