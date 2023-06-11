namespace PubSubPattern.Subscriber.Models;

public record MessageReadDto(int Id, string? TopicMessage, DateTime ExpiresAfter, string? MessageStatus);