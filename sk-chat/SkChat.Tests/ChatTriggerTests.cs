using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Http;
using SkChat;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.AI; // For OpenAIPromptExecutionSettings, ChatRole
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;

namespace SkChat.Tests;

public class ChatTriggerTests
{
    private readonly Mock<ILogger<ChatTrigger>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IOptions<JsonSerializerOptions> _optionsJson;

    public ChatTriggerTests()
    {
        _logger = new Mock<ILogger<ChatTrigger>>();


        _jsonOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        _jsonOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        _optionsJson = Options.Create(_jsonOptions);
    }

    private ServiceProvider CreateServiceProviderWithServices(IChatClient chatClient)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new WorkerOptions { Serializer = new JsonObjectSerializer(_jsonOptions) }));
        services.AddKeyedSingleton<IChatClient>("openAiChatClient", chatClient);
        services.AddSingleton<ObjectSerializer>(new JsonObjectSerializer(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return services.BuildServiceProvider();
    }


    [Fact]
    public async Task ReturnsOkAndStreamsContent_ForValidPayload()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        // Set up HttpContext for streaming response
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);

        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        req.Body.Write(payloadBytes, 0, payloadBytes.Length);
        req.Body.Seek(0, SeekOrigin.Begin);

        // Mock the chat client
        var streamingResults = GetStreamingResults(new[] { "Hi", null, " there!" });
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(streamingResults);

        // Capture logger invocations
        var logInvocations = new List<object?>();
        _logger.Setup(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                logInvocations.Add(invocation.Arguments[2]);
            }));

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal("text/event-stream", httpContext.Response.Headers["Content-Type"]);
        Assert.Contains("Hi", body);
        Assert.Contains("there!", body);
        Assert.Contains(logInvocations, v => v != null && v.ToString() != null && v.ToString()!.Contains("Received request body"));
    }

    [Fact]
    public async Task ReturnsBadRequest_ForNullPayload()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);
        var req = new FakeHttpRequestData(context);

        // Act
        await function.Run(req, null!);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.Contains("Model is null", body);
    }

    [Fact]
    public async Task ReturnsBadRequest_ForEmptyPrompt()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);
        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, ""), History = new ChatMessage[0] };
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        req.Body.Write(payloadBytes, 0, payloadBytes.Length);
        req.Body.Seek(0, SeekOrigin.Begin);

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task OnlyNonNullContentIsStreamed()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);
        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        req.Body.Write(payloadBytes, 0, payloadBytes.Length);
        req.Body.Seek(0, SeekOrigin.Begin);

        // Mock the chat client
        var streamingResults = GetStreamingResults(new[] { null, "A", null, "B" });
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(streamingResults);

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Contains("A", body);
        Assert.Contains("B", body);
        Assert.DoesNotContain("null", body);
    }

    [Fact]
    public async Task ReturnsEarly_WhenHttpContextIsNull()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        var context = new FakeFunctionContext(provider);
        // Don't set HttpContext, so it remains null
        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };

        // Act
        await function.Run(req, payload);

        // Assert
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HttpContext is null")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify chat client was never called
        chatClientMock.Verify(
            s => s.GetStreamingResponseAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandlesRequestCancellation_DuringStreaming()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        // Set up cancellation token that will be cancelled
        var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;

        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);

        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };

        // Mock the chat client to return content that triggers cancellation check
        var streamingResults = GetStreamingResultsWithCancellation(new[] { "Start", "Cancel here" }, cts);
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(streamingResults);

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Contains("Start", body);
        Assert.Contains("event: done", body);
        Assert.Contains("[DONE]", body);

        // Verify cancellation was logged
        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request was cancelled by the client")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandlesOperationCancelledException()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);

        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };

        // Mock the chat client to throw OperationCanceledException
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException("Operation was cancelled"));

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Contains("event: done", body);
        Assert.Contains("[DONE]", body);

        // Verify cancellation was logged
        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("The operation was canceled by the client")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandlesGeneralException_SendsErrorEvent()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);

        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };

        // Mock the chat client to throw a general exception
        var expectedException = new InvalidOperationException("Something went wrong");
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Throws(expectedException);

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Contains("event: error", body);
        Assert.Contains("An internal server error occurred", body);
        Assert.Contains("event: done", body);
        Assert.Contains("[DONE]", body);

        // Verify error was logged
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An error occurred during the streaming chat")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AlwaysSendsDoneEvent_InFinallyBlock()
    {
        // Arrange
        var chatClientMock = new Mock<IChatClient>();
        var provider = CreateServiceProviderWithServices(chatClientMock.Object);
        var kernel = new Kernel(provider);
        var function = new ChatTrigger(_logger.Object, kernel, _optionsJson);

        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var context = new FakeFunctionContext(provider);
        context.SetHttpContext(httpContext);

        var req = new FakeHttpRequestData(context);
        var payload = new ChatPayload { Utterance = new ChatMessage(ChatRole.User, "Hello"), History = new[] { new ChatMessage(ChatRole.User, "Hi there") } };

        // Mock the chat client with empty results
        var streamingResults = GetStreamingResults(new string[0]);
        chatClientMock.Setup(s => s.GetStreamingResponseAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(streamingResults);

        // Act
        await function.Run(req, payload);

        // Assert
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Contains("event: done", body);
        Assert.Contains("[DONE]", body);
    }

    // Helper to create an async stream of streaming response updates
    private static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResults(IEnumerable<string?> contents)
    {
        foreach (var content in contents)
        {
            await Task.Delay(1);
            var update = new ChatResponseUpdate
            {
                Contents = content != null ? new List<AIContent>
                {
                    new Microsoft.Extensions.AI.TextContent(content)
                } : new List<AIContent>()
            };
            yield return update;
        }
    }

    // Helper to create streaming results that trigger cancellation
    private static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResultsWithCancellation(
        IEnumerable<string?> contents,
        CancellationTokenSource cts)
    {
        var contentList = contents.ToList();
        for (int i = 0; i < contentList.Count; i++)
        {
            var content = contentList[i];

            // Cancel after first content
            if (i == 1)
            {
                cts.Cancel();
            }

            await Task.Delay(1);
            var update = new ChatResponseUpdate
            {
                Contents = content != null ? new List<AIContent>
                {
                    new Microsoft.Extensions.AI.TextContent(content)
                } : new List<AIContent>()
            };
            yield return update;
        }
    }

}
