using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalendarAPI.Models;

public class EventParticipant
{
    public int Id { get; set; }
    
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public bool IsRequired { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int Revision { get; set; }
} 