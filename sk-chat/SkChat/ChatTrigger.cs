using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;


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
    public async Task Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [FromBody] ChatPayload chatPayload)
    {
        var httpContext = req.FunctionContext.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogError("HttpContext is null. Cannot process the request.");
            // In a real app, you'd want a more robust error response here.
            return;
        }

        var response = httpContext.Response;
        var cancellationToken = httpContext.RequestAborted;

        var (isValid, errors) = ModelValidator.Validate(chatPayload);

        if (!isValid)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsJsonAsync(errors, cancellationToken);
            return;
        }

        _logger.LogInformation("Received request body: {ChatPayload}", chatPayload);

        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a helpful assistant. Always reply in markdown format.")
        };

        chatMessages.AddRange(chatPayload.History);
        chatMessages.Add(chatPayload.Utterance);

        var chatClient = _kernel.GetRequiredService<IChatClient>("openAiChatClient");

        response.StatusCode = StatusCodes.Status200OK;
        response.Headers["Content-Type"] = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";

        var serializerOptions = new JsonSerializerOptions(_jsonSerializerOptions.Value)
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        try
        {
            await foreach (var streamingResult in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Request was cancelled by the client.");
                    break;
                }

                if (streamingResult.Contents?.Count > 0)
                {
                    var content = string.Join(
                        "",
                        streamingResult.Contents
                            .OfType<Microsoft.Extensions.AI.TextContent>()
                            .Select(tc => tc.Text)
                    );

                    if (!string.IsNullOrEmpty(content))
                    {
                        var chatMessage = new ChatMessage(ChatRole.Assistant, content)
                        {
                            MessageId = streamingResult.MessageId
                        };

                        var jsonResponse = JsonSerializer.Serialize(chatMessage, new JsonSerializerOptions(_jsonSerializerOptions.Value)
                        {
                            WriteIndented = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });


                        await response.WriteAsync($"data: {jsonResponse}\n\n", cancellationToken);
                        await response.Body.FlushAsync(cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("The operation was canceled by the client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the streaming chat.");
            // It's not possible to change the status code after the response has started streaming.
            // Instead, we send a custom 'error' event to the client.
            var errorPayload = JsonSerializer.Serialize(new { error = "An internal server error occurred." }, serializerOptions);
            await response.WriteAsync($"event: error\ndata: {errorPayload}\n\n", CancellationToken.None);
            await response.Body.FlushAsync(CancellationToken.None);
        }
        finally
        {
            // Signal the end of the stream to the client, even if an error occurred.
            await response.WriteAsync("event: done\ndata: [DONE]\n\n", CancellationToken.None);
            await response.Body.FlushAsync(CancellationToken.None);
        }
    }
}

