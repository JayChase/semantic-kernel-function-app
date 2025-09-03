using System.Text.Json;
using System.Text.Json.Serialization;
using SkChat.Middleware;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.Azure.Functions.Worker;


var builder = FunctionsApplication.CreateBuilder(args);

// Configure ASP.NET Core integration
builder.ConfigureFunctionsWebApplication();

builder.Configuration.AddEnvironmentVariables();

builder.UseMiddleware<ExceptionHandlingMiddleware>();

builder.Services.Configure<JsonSerializerOptions>(options =>
        {
            options.AllowTrailingCommas = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNameCaseInsensitive = true;
            options.WriteIndented = true;
        });



builder.Services.AddOptions<AiApiConfig>("openAiConfig")
.BindWithPascalCaseKeys(builder.Configuration, "AZURE_OPENAI_API").ValidateDataAnnotations().ValidateOnStart();

// Add Application Insights services
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddApplicationInsights();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
});



builder.Services.AddAzureOpenAIChatClient("openAiChatClient", "openAiConfig");

builder.Services.AddTransient((serviceProvider) =>
{
    var kernel = new Kernel(serviceProvider);
    return kernel;
});


builder.Services.AddChatService(ChatServiceType.OpenAiChat);

builder.Build().Run();
