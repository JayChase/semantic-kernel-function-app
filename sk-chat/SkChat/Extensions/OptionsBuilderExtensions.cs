using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

public static class OptionsBuilderExtensions
{
    public static OptionsBuilder<T> BindWithPascalCaseKeys<T>(this OptionsBuilder<T> builder, IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        var pascalCaseData = section.GetChildren()
            .Select(c => new KeyValuePair<string, string?>(ToPascalCase(c.Key), c.Value))
            .ToList(); // Ensures compatibility with AddInMemoryCollection

        var memoryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(pascalCaseData)
            .Build();

        return builder.Bind(memoryConfig);
    }

    private static string ToPascalCase(string input)
    {
        var parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.Append(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part.ToLowerInvariant()));
        }
        return sb.ToString();
    }
}
