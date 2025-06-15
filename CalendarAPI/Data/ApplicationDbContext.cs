using Microsoft.EntityFrameworkCore;
using CalendarAPI.Models;

namespace CalendarAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventParticipant> EventParticipants { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<EventParticipant>()
            .HasOne(ep => ep.Event)
            .WithMany(e => e.Participants)
            .HasForeignKey(ep => ep.EventId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<EventParticipant>()
            .HasOne(ep => ep.User)
            .WithMany(u => u.EventParticipants)
            .HasForeignKey(ep => ep.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
} 