using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;

public class ChatPayload
{

    [Required]
    public required ChatMessage Utterance { get; set; }

    public IEnumerable<ChatMessage> History { get; set; } = [];
}
