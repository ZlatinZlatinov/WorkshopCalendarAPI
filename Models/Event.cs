using System.ComponentModel.DataAnnotations;

namespace WorkshopCalendarAPI.Models;

public class Event
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartTime { get; set; }
    
    [Required]
    public DateTime EndTime { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsCancelled { get; set; }
    
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
}