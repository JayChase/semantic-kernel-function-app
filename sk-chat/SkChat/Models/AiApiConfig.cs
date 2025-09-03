using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class AiApiConfig
{

    [Required]
    public string DeploymentName { get; set; } = string.Empty;

    [Required]
    public string Endpoint { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
}
