
using SkChat.ServiceFactories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Options;
using Azure.Identity;

public enum ChatServiceType
{
    DeepseekChat,
    OpenAiChat
}


public static partial class SkCollectionExtensions
{

    public static IServiceCollection AddChatService(
        this IServiceCollection services,
        ChatServiceType serviceType = ChatServiceType.OpenAiChat
    )
    {
        services.AddTransient(ServiceFactories.SkChatKernelFactory());
        return services;
    }

    public static IServiceCollection AddAzureOpenAIChatClient(
  this IServiceCollection services, string serviceId = "openAiChatClient", string aiConfigName = "openAiConfig")
    {
        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AiApiConfig>>();
        AiApiConfig options = optionsMonitor.Get(aiConfigName);

        var credentials = new DefaultAzureCredential();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        return services.AddAzureOpenAIChatClient(
            options.DeploymentName,
            options.Endpoint,
            credentials,
            serviceId: serviceId,
            apiVersion: null,
            httpClient: null,
            openTelemetrySourceName: "SkChat.AzureOpenAIChatClient",
            openTelemetryConfig: null);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }
}
