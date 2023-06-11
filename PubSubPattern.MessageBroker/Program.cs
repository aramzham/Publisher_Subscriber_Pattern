using Microsoft.EntityFrameworkCore;
using PubSubPattern.MessageBroker;
using PubSubPattern.MessageBroker.Data;
using PubSubPattern.MessageBroker.Data.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<AppDbContext>(o => o.UseSqlite("Data source=MessageBroker.db"));

var app = builder.Build();

app.UseHttpsRedirection();

// create a topic
app.MapPost("/topics", async (AppDbContext db, Topic topic) =>
{
    await db.Topics.AddAsync(topic);
    await db.SaveChangesAsync();
    
    return Results.Created($"/topics/{topic.Id}", topic);
});

// get all topics
app.MapGet("/topics", async (AppDbContext db) =>
{
    var topics = await db.Topics.ToListAsync();
    return Results.Ok(topics);
});

// publish a message
app.MapPost("/topics/{topicId}/messages", async (AppDbContext db, int topicId, Message message) =>
{
    var anyTopic = await db.Topics.AnyAsync(x => x.Id == topicId);
    if (!anyTopic)
        return Results.NotFound("no topic found");

    var subs = db.Subscriptions.Where(x => x.TopicId == topicId);
    if (!subs.Any())
        return Results.NotFound("no subscription found for this topic");

    foreach (var sub in subs)
    {
        await db.Messages.AddAsync(new Message()
        {
            Status = MessageStatus.Published,
            SubscriptionId = sub.Id,
            TopicMessage = message.TopicMessage,
            ExpiresAfter = message.ExpiresAfter
        });
    }

    await db.SaveChangesAsync();

    return Results.Ok("message has been published");
});

// create a subscription
app.MapPost("/topics/{topicId}/subscriptions", async (AppDbContext db, Subscription subscription, int topicId) =>
{
    var anyTopic = await db.Topics.AnyAsync(x => x.Id == topicId);
    if (!anyTopic)
        return Results.NotFound("no topic found");

    subscription.TopicId = topicId;

    await db.Subscriptions.AddAsync(subscription);
    await db.SaveChangesAsync();

    return Results.Created($"/topics/{topicId}/subscriptions/{subscription.Id}", subscription);
});

// get subscriber messages
app.MapGet("/subscriptions/{subsId}/messages", async (AppDbContext db, int subsId) =>
{
    var anySub = await db.Subscriptions.AnyAsync(x => x.Id == subsId);
    if (!anySub)
        return Results.NotFound("no subscription found");

    var messages = db.Messages.Where(x => x.SubscriptionId == subsId && x.Status != MessageStatus.Sent);
    if (!await messages.AnyAsync())
        return Results.NotFound("no new messages");

    foreach (var message in messages)
    {
        message.Status = MessageStatus.Requested;
    }

    await db.SaveChangesAsync();

    return Results.Ok(messages);
});

// acknowledge message for subscriber
app.MapPost("/subscriptions/{subscriptionId}/messages", async (AppDbContext db, int subscriptionId, int[] confirmations) =>
{
    var anySub = await db.Subscriptions.AnyAsync(x => x.Id == subscriptionId);
    if (!anySub)
        return Results.NotFound("no subscription found");

    if (!confirmations.Any())
        return Results.BadRequest();

    var messages = db.Messages.Where(x => confirmations.Contains(x.Id));
    foreach (var m in messages)
    {
        m.Status = MessageStatus.Sent;
    }

    await db.SaveChangesAsync();

    return Results.Ok($"Acknowledged {messages.Count()}/{confirmations.Length}");
});

app.Run();
