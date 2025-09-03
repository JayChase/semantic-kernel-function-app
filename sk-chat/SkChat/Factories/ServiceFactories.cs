using Microsoft.SemanticKernel;

namespace SkChat.ServiceFactories;

public static class ServiceFactories
{
    /// <summary>
    /// Creates a factory method to instantiate a Kernel with the provided IServiceProvider.
    /// </summary>
    /// <returns>A function that takes an IServiceProvider and returns a Kernel.</returns>
    public static Func<IServiceProvider, Kernel> SkChatKernelFactory()
    {
        return (provider) =>
        {
            // Create a new Kernel instance with the provided service provider
            var kernel = new Kernel(provider);
            // Optionally, you can add additional services or configurations to the kernel here
            return kernel;
        };
    }
}
