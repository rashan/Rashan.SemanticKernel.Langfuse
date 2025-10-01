using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Rashan.SemanticKernel.Langfuse.Extensions;
using Rashan.SemanticKernel.Langfuse.Models;

namespace ChatClient;

public class Program
{
    private static readonly List<string> _conversationHistory = [];
    
    public static async Task Main(string[] args)
    {
        // Display welcome message
        DisplayWelcome();

        try
        {
            // Build the host with dependency injection
            var host = CreateHost();
            
            // Validate configuration
            await ValidateConfigurationAsync(host);
            
            // Get the configured kernel
            var kernel = host.Services.GetRequiredService<Kernel>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Chat client initialized successfully");
            
            // Start the interactive chat loop
            await RunChatLoopAsync(kernel, logger);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.ResetColor();
            
            if (ex is InvalidOperationException)
            {
                Console.WriteLine("\nPlease check your configuration in appsettings.json or environment variables.");
                Console.WriteLine("Required settings:");
                Console.WriteLine("- Langfuse.PublicKey (or LANGFUSE_PUBLIC_KEY)");
                Console.WriteLine("- Langfuse.SecretKey (or LANGFUSE_SECRET_KEY)");
                Console.WriteLine("- OpenAI.ApiKey (or OPENAI_API_KEY)");
            }
            
            Environment.Exit(1);
        }
    }

    private static void DisplayWelcome()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          Langfuse SemanticKernel Chat Demo                  ║");
        Console.WriteLine("║          Demonstrating OpenTelemetry Integration            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();
        
        // Configure configuration sources
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddFilter("Microsoft.SemanticKernel", LogLevel.Warning);
            logging.AddFilter("System.Net.Http", LogLevel.Warning);
        });

        // Get configuration values
        var langfusePublicKey = builder.Configuration["Langfuse:PublicKey"] ?? 
                               Environment.GetEnvironmentVariable("LANGFUSE_PUBLIC_KEY");
        var langfuseSecretKey = builder.Configuration["Langfuse:SecretKey"] ?? 
                               Environment.GetEnvironmentVariable("LANGFUSE_SECRET_KEY");
        var langfuseEndpoint = builder.Configuration["Langfuse:Endpoint"] ?? "https://cloud.langfuse.com";
        
        var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? 
                          Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        // Validate required configuration
        if (string.IsNullOrEmpty(langfusePublicKey))
            throw new InvalidOperationException("Langfuse PublicKey is required");
        if (string.IsNullOrEmpty(langfuseSecretKey))
            throw new InvalidOperationException("Langfuse SecretKey is required");
        if (string.IsNullOrEmpty(openAiApiKey))
            throw new InvalidOperationException("OpenAI ApiKey is required");

        // Add Langfuse integration with OpenTelemetry
        builder.Services.AddLangfuseIntegration(
            publicKey: langfusePublicKey,
            secretKey: langfuseSecretKey,
            endpoint: langfuseEndpoint
        );

        // Add Semantic Kernel with OpenAI
        builder.Services.AddKernel()
            .AddOpenAIChatCompletion(
                modelId: openAiModel,
                apiKey: openAiApiKey
            );

        return builder.Build();
    }

    private static async Task ValidateConfigurationAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        
        // Test Langfuse connection
        Console.Write("Checking Langfuse connection... ");
        try
        {
            // This will be traced automatically
            var kernel = host.Services.GetRequiredService<Kernel>();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Connected");
            Console.ResetColor();
            
            logger.LogInformation("Langfuse integration validated successfully");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Failed");
            Console.ResetColor();
            throw new InvalidOperationException($"Failed to validate Langfuse connection: {ex.Message}", ex);
        }

        // Test AI model
        Console.Write("Checking AI model... ");
        try
        {
            var kernel = host.Services.GetRequiredService<Kernel>();
            await kernel.InvokePromptAsync("Hello");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Ready");
            Console.ResetColor();
            
            var model = configuration["OpenAI:Model"];
            logger.LogInformation("AI model {Model} validated successfully", model);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Failed");
            Console.ResetColor();
            throw new InvalidOperationException($"Failed to validate AI model: {ex.Message}", ex);
        }

        Console.WriteLine();
    }

    private static async Task RunChatLoopAsync(Kernel kernel, ILogger logger)
    {
        Console.WriteLine("Chat started! Type your message and press Enter.");
        Console.WriteLine("Available commands: /help, /clear, /history, /exit");
        Console.WriteLine();

        var langfuseEndpoint = kernel.Services.GetRequiredService<IConfiguration>()["Langfuse:Endpoint"];
        
        while (true)
        {
            // Get user input
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("You: ");
            Console.ResetColor();
            
            var userInput = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            // Handle commands
            if (userInput.StartsWith('/'))
            {
                if (await HandleCommandAsync(userInput, langfuseEndpoint))
                    break; // Exit command
                continue;
            }

            try
            {
                // Add user message to history
                _conversationHistory.Add($"User: {userInput}");
                
                // Create a conversation context
                var conversationContext = string.Join("\n", _conversationHistory.TakeLast(10));
                var prompt = $"""
                    You are a helpful AI assistant. Please respond to the user's message in a conversational way.
                    
                    Conversation history:
                    {conversationContext}
                    
                    Please respond to the latest user message.
                    """;

                // Get AI response (this will be automatically traced to Langfuse)
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("AI: ");
                Console.ResetColor();
                
                var response = await kernel.InvokePromptAsync(prompt);
                var aiResponse = response.ToString();
                
                Console.WriteLine(aiResponse);
                Console.WriteLine();
                
                // Add AI response to history
                _conversationHistory.Add($"AI: {aiResponse}");
                
                logger.LogDebug("Processed user message and generated response");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                logger.LogError(ex, "Error processing user message");
            }
        }
    }

    private static async Task<bool> HandleCommandAsync(string command, string? langfuseEndpoint)
    {
        return command.ToLowerInvariant() switch
        {
            "/help" => HandleHelpCommand(),
            "/clear" => HandleClearCommand(),
            "/history" => HandleHistoryCommand(),
            "/exit" => HandleExitCommand(langfuseEndpoint),
            _ => HandleUnknownCommand(command)
        };
    }

    private static bool HandleHelpCommand()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Available commands:");
        Console.WriteLine("  /help     - Show this help message");
        Console.WriteLine("  /clear    - Clear conversation history");
        Console.WriteLine("  /history  - Show conversation history");
        Console.WriteLine("  /exit     - Exit the application");
        Console.ResetColor();
        Console.WriteLine();
        return false;
    }

    private static bool HandleClearCommand()
    {
        _conversationHistory.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Conversation history cleared.");
        Console.ResetColor();
        Console.WriteLine();
        return false;
    }

    private static bool HandleHistoryCommand()
    {
        if (_conversationHistory.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No conversation history.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Conversation History:");
            Console.ResetColor();
            foreach (var message in _conversationHistory)
            {
                Console.WriteLine($"  {message}");
            }
        }
        Console.WriteLine();
        return false;
    }

    private static bool HandleExitCommand(string? langfuseEndpoint)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Thanks for trying the Langfuse SemanticKernel integration!");
        if (!string.IsNullOrEmpty(langfuseEndpoint))
        {
            Console.WriteLine($"View your conversation traces at: {langfuseEndpoint}");
        }
        Console.ResetColor();
        return true;
    }

    private static bool HandleUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Type /help for available commands.");
        Console.ResetColor();
        Console.WriteLine();
        return false;
    }
}
