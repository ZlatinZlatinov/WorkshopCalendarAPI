using WorkshopCalendarAPI.Models;

namespace WorkshopCalendarAPI.Services;

public class FreeSlotsService
{
    public IEnumerable<TimeSlot> FindFreeSlots(
        IEnumerable<Event> existingEvents,
        DateTime startDate,
        DateTime endDate,
        TimeSpan duration,
        IEnumerable<int> participantIds)
    {
        var slots = new List<TimeSlot>();
        var currentTime = startDate;

        // Get all events for the participants in the given time range
        var relevantEvents = existingEvents
            .Where(e => !e.IsCancelled &&
                       e.Participants.Any(p => participantIds.Contains(p.UserId)) &&
                       e.StartTime < endDate &&
                       e.EndTime > startDate)
            .OrderBy(e => e.StartTime)
            .ToList();

        while (currentTime < endDate)
        {
            var slotEnd = currentTime.Add(duration);
            if (slotEnd > endDate) break;

            // Check if this slot overlaps with any existing events
            var isSlotFree = !relevantEvents.Any(e =>
                (e.StartTime <= currentTime && e.EndTime > currentTime) || // Event starts before or during slot
                (e.StartTime < slotEnd && e.EndTime >= slotEnd) || // Event ends after or during slot
                (e.StartTime >= currentTime && e.EndTime <= slotEnd)); // Event is completely within slot

            if (isSlotFree)
            {
                slots.Add(new TimeSlot
                {
                    StartTime = currentTime,
                    EndTime = slotEnd
                });
            }

            // Move to next slot
            currentTime = currentTime.AddMinutes(30); // 30-minute intervals
        }

        return slots;
    }
}

public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
} 