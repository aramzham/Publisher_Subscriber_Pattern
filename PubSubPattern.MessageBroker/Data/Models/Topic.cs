using System.ComponentModel.DataAnnotations;

namespace PubSubPattern.MessageBroker.Data.Models;

public class Topic
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string? Name { get; set; }
}