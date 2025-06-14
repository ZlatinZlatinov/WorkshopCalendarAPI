using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopCalendarAPI.Data;
using WorkshopCalendarAPI.Models;
using WorkshopCalendarAPI.Services;

namespace WorkshopCalendarAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly FreeSlotsService _freeSlotsService;

    public EventsController(ApplicationDbContext context, FreeSlotsService freeSlotsService)
    {
        _context = context;
        _freeSlotsService = freeSlotsService;
    }

    // GET: api/v1/events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(e => e.StartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EndTime <= endDate.Value);
        }

        return await query.ToListAsync();
    }

    // GET: api/v1/events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var @event = await _context.Events
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (@event == null)
        {
            return NotFound();
        }

        return @event;
    }

    // POST: api/v1/events
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent(Event @event)
    {
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
    }

    // PUT: api/v1/events/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, Event @event)
    {
        if (id != @event.Id)
        {
            return BadRequest();
        }

        @event.UpdatedAt = DateTime.UtcNow;
        _context.Entry(@event).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/v1/events/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound();
        }

        @event.IsCancelled = true;
        @event.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/v1/events/5/participants
    [HttpGet("{eventId}/participants")]
    public async Task<ActionResult<IEnumerable<EventParticipant>>> GetEventParticipants(int eventId)
    {
        return await _context.EventParticipants
            .Include(p => p.User)
            .Where(p => p.EventId == eventId)
            .ToListAsync();
    }

    // POST: api/v1/events/5/participants
    [HttpPost("{eventId}/participants")]
    public async Task<ActionResult<EventParticipant>> AddParticipant(int eventId, EventParticipant participant)
    {
        if (eventId != participant.EventId)
        {
            return BadRequest();
        }

        _context.EventParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventParticipants), new { eventId = participant.EventId }, participant);
    }

    // PUT: api/v1/events/5/participants/1
    [HttpPut("{eventId}/participants/{userId}")]
    public async Task<IActionResult> UpdateParticipant(int eventId, int userId, EventParticipant participant)
    {
        if (eventId != participant.EventId || userId != participant.UserId)
        {
            return BadRequest();
        }

        participant.Revision++;
        _context.Entry(participant).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EventParticipantExists(eventId, userId))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/v1/events/5/participants/1
    [HttpDelete("{eventId}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(int eventId, int userId)
    {
        var participant = await _context.EventParticipants
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

        if (participant == null)
        {
            return NotFound();
        }

        _context.EventParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/v1/events/free-slots
    [HttpGet("free-slots")]
    public async Task<ActionResult<IEnumerable<TimeSlot>>> FindFreeSlots(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int durationMinutes,
        [FromQuery] int[] participantIds)
    {
        var events = await _context.Events
            .Include(e => e.Participants)
            .Where(e => e.StartTime < endDate && e.EndTime > startDate)
            .ToListAsync();

        var slots = _freeSlotsService.FindFreeSlots(
            events,
            startDate,
            endDate,
            TimeSpan.FromMinutes(durationMinutes),
            participantIds);

        return Ok(slots);
    }

    private bool EventExists(int id)
    {
        return _context.Events.Any(e => e.Id == id);
    }

    private bool EventParticipantExists(int eventId, int userId)
    {
        return _context.EventParticipants.Any(p => p.EventId == eventId && p.UserId == userId);
    }
} 