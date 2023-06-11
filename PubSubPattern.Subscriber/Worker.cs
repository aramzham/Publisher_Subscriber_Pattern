using System.Net.Http.Json;
using PubSubPattern.Subscriber.Models;

namespace PubSubPattern.Subscriber;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly int _subscriptionId;

    public Worker(ILogger<Worker> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration.GetValue<string>("BaseAddress"));
        
        _subscriptionId = configuration.GetValue<int>("SubscriptionId");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            var messages = await GetMessages();
            if (messages.Any())
            {
                await AcknowledgeMessages(_httpClient, messages);
            }
            
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<List<int>> GetMessages()
    {
        var ackIds = new List<int>();
        var newMessages = new List<MessageReadDto>();

        try
        {
            newMessages = await _httpClient.GetFromJsonAsync<List<MessageReadDto>>($"subscriptions/{_subscriptionId}/messages");
        }
        catch
        {
            return ackIds;
        }

        foreach (var msg in newMessages)
        {
            Console.WriteLine($"{msg.Id} - {msg.TopicMessage} - {msg.MessageStatus}");
            ackIds.Add(msg.Id);
        }

        return ackIds;
    }

    private async Task AcknowledgeMessages(HttpClient httpClient, IEnumerable<int> ackIds)
    {
        var response = await httpClient.PostAsJsonAsync($"subscriptions/{_subscriptionId}/messages", ackIds);
        var returnMessage = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine(returnMessage);
    }
}