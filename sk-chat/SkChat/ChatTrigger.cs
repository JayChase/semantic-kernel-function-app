using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using Microsoft.SemanticKernel.Connectors.OpenAI;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;
using Microsoft.Azure.Functions.Worker.Http;

using Microsoft.Extensions.AI;
using System.Text.Json;


namespace SkChat;

public class ChatTrigger
{
    private readonly ILogger<ChatTrigger> _logger;


    private readonly Kernel _kernel;

    private readonly IOptions<JsonSerializerOptions> _jsonSerializerOptions;

    public ChatTrigger(ILogger<ChatTrigger> logger, Kernel kernel, IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        _logger = logger;
        _kernel = kernel;
        _jsonSerializerOptions = jsonSerializerOptions;
    }


    [Function("chat")]
    public async Task Run([HttpTrigger(AuthorizationLevel.Function, "post")] FunctionContext context, [FromBody] ChatPayload chatPayload)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null. Cannot process the request.");
            return;
        }

        var response = httpContext.Response;

        var (isValid, errors) = ModelValidator.Validate(chatPayload);

        if (!isValid)
        {
            response.StatusCode = 400;
            await response.WriteAsJsonAsync(errors);

        }
        else
        {


            _logger.LogInformation("Received request body: {ChatPayload}", chatPayload);

            //short cut empty requests with a placeholder response

            // Add the history
            var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant. Always reply in markdown format.")
        };

            chatMessages.AddRange(chatPayload.History);
            //https://devblogs.microsoft.com/semantic-kernel/semantic-kernel-and-microsoft-extensions-ai-better-together-part-2/

            // Add user input        
            chatMessages.Add(chatPayload.Utterance);

            // Initialize the chat completion service based on the configured provider
            var chatClient = _kernel.GetRequiredService<IChatClient>("openAiChatClient");
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };



            //TODO now get this stream json properly like UAI 👇 there must be a plugin for that?
            // https://devblogs.microsoft.com/semantic-kernel/using-json-schema-for-structured-output-in-net-for-openai-models/
            // https://dev.to/stormhub/openai-chat-completion-with-json-output-format-l5i
            // Set the response headers for SSE
            response.StatusCode = 200;
            response.Headers["Content-Type"] = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";

            await using var writer = new StreamWriter(response.Body);

            await foreach (var streamingResult in chatClient.GetStreamingResponseAsync(chatMessages))
            {
                if (streamingResult.Contents != null && streamingResult.Contents.Count > 0)
                {
                    var content = string.Join(
                        "",
                        streamingResult.Contents
                            .OfType<Microsoft.Extensions.AI.TextContent>()
                            .Select(tc => tc.Text)
                    );
                    _logger.LogInformation("Streaming content: {Content}", content);

                    var chatMessage = new ChatMessage(ChatRole.Assistant, content);
                    chatMessage.MessageId = streamingResult.MessageId;
                    var jsonResponse = JsonSerializer.Serialize(chatMessage, new JsonSerializerOptions(_jsonSerializerOptions.Value)
                    {
                        WriteIndented = false,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    await writer.WriteAsync($"{jsonResponse}\n");
                    await writer.FlushAsync();

                }
            }


        }
    }
}

