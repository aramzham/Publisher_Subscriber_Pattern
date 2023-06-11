using Microsoft.EntityFrameworkCore;
using PubSubPattern.MessageBroker.Data.Models;

namespace PubSubPattern.MessageBroker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
}