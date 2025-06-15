using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using CalendarAPI.Controllers;
using CalendarAPI.Data;
using CalendarAPI.Models;
using CalendarAPI.Services;

namespace CalendarAPI.Tests;

public class EventsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventsController _controller;
    private readonly FreeSlotsService _freeSlotsService;

    public EventsControllerTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _freeSlotsService = new FreeSlotsService();
        _controller = new EventsController(_context, _freeSlotsService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetEvents_ReturnsAllEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event
            {
                Title = "Event 1",
                Description = "Description 1",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                IsCancelled = false
            },
            new Event
            {
                Title = "Event 2",
                Description = "Description 2",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                IsCancelled = true
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvents(null, null);

        // Assert
        var okResult = Assert.IsType<ActionResult<IEnumerable<Event>>>(result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<Event>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetEvent_WithValidId_ReturnsEvent()
    {
        // Arrange
        var @event = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };
        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvent(@event.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<Event>>(result);
        var returnValue = Assert.IsType<Event>(okResult.Value);
        Assert.Equal(@event.Id, returnValue.Id);
        Assert.Equal(@event.Title, returnValue.Title);
        Assert.Equal(@event.Description, returnValue.Description);
        Assert.Equal(@event.StartTime, returnValue.StartTime);
        Assert.Equal(@event.EndTime, returnValue.EndTime);
        Assert.Equal(@event.IsCancelled, returnValue.IsCancelled);
        Assert.Empty(returnValue.Participants);
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetEvent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateEvent_WithValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var @event = new Event
        {
            Title = "New Event",
            Description = "New Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };

        // Act
        var result = await _controller.CreateEvent(@event);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<Event>(createdResult.Value);
        Assert.Equal(@event.Title, returnValue.Title);
        Assert.Equal(@event.Description, returnValue.Description);
        Assert.Equal(@event.StartTime, returnValue.StartTime);
        Assert.Equal(@event.EndTime, returnValue.EndTime);
        Assert.Equal(@event.IsCancelled, returnValue.IsCancelled);
        Assert.Empty(returnValue.Participants);
    }

    [Theory]
    [InlineData("", "Test Description", "2024-03-01T09:00:00Z", "2024-03-01T10:00:00Z")]
    [InlineData("Test Event", "", "2024-03-01T09:00:00Z", "2024-03-01T10:00:00Z")]
    [InlineData("Test Event", "Test Description", "2024-03-01T10:00:00Z", "2024-03-01T09:00:00Z")]
    public async Task CreateEvent_WithInvalidData_ReturnsBadRequest(
        string title, string description, string startTime, string endTime)
    {
        // Arrange
        var @event = new Event
        {
            Title = title,
            Description = description,
            StartTime = DateTime.Parse(startTime),
            EndTime = DateTime.Parse(endTime),
            IsCancelled = false
        };

        // Act
        var result = await _controller.CreateEvent(@event);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task UpdateEvent_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var @event = new Event
        {
            Title = "Original Event",
            Description = "Original Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };
        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();

        @event.Title = "Updated Event";
        @event.Description = "Updated Description";

        // Act
        var result = await _controller.UpdateEvent(@event.Id, @event);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var updatedEvent = await _context.Events.FindAsync(@event.Id);
        Assert.NotNull(updatedEvent);
        Assert.Equal("Updated Event", updatedEvent.Title);
        Assert.Equal("Updated Description", updatedEvent.Description);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var @event = new Event
        {
            Id = 999,
            Title = "Test Event",
            Description = "Test Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };

        // Act
        var result = await _controller.UpdateEvent(999, @event);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateEvent_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var @event = new Event
        {
            Id = 1,
            Title = "Test Event",
            Description = "Test Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };

        // Act
        var result = await _controller.UpdateEvent(2, @event);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var @event = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };
        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteEvent(@event.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var deletedEvent = await _context.Events.FindAsync(@event.Id);
        Assert.NotNull(deletedEvent);
        Assert.True(deletedEvent.IsCancelled);
    }

    [Fact]
    public async Task DeleteEvent_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteEvent(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEvent_WithParticipants_ReturnsEventWithParticipants()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var @event = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            IsCancelled = false
        };
        await _context.Events.AddAsync(@event);

        var participant = new EventParticipant
        {
            EventId = @event.Id,
            UserId = user.Id,
            IsRequired = true,
            CreatedAt = DateTime.UtcNow,
            Revision = 1
        };
        await _context.EventParticipants.AddAsync(participant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetEvent(@event.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<Event>>(result);
        var returnValue = Assert.IsType<Event>(okResult.Value);
        Assert.Single(returnValue.Participants);
        var returnedParticipant = returnValue.Participants.First();
        Assert.Equal(user.Id, returnedParticipant.UserId);
        Assert.Equal(@event.Id, returnedParticipant.EventId);
        Assert.True(returnedParticipant.IsRequired);
        Assert.Equal(1, returnedParticipant.Revision);
    }

    [Fact]
    public async Task FindFreeSlots_ReturnsAvailableSlots()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddHours(2);
        var durationMinutes = 30;
        var participantIds = new[] { 1 };

        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);

        var @event = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartTime = startDate.AddMinutes(30),
            EndTime = startDate.AddMinutes(60),
            IsCancelled = false,
            Participants = new List<EventParticipant>
            {
                new EventParticipant
                {
                    UserId = user.Id,
                    IsRequired = true,
                    CreatedAt = DateTime.UtcNow,
                    Revision = 1
                }
            }
        };
        await _context.Events.AddAsync(@event);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.FindFreeSlots(startDate, endDate, durationMinutes, participantIds);

        // Assert
        var okResult = Assert.IsType<ActionResult<IEnumerable<TimeSlot>>>(result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<TimeSlot>>(okResult.Value);
        var slots = returnValue.ToList();
        Assert.Equal(2, slots.Count);
        Assert.Equal(startDate, slots[0].StartTime);
        Assert.Equal(startDate.AddMinutes(durationMinutes), slots[0].EndTime);
        Assert.Equal(endDate.AddMinutes(-durationMinutes), slots[1].StartTime);
        Assert.Equal(endDate, slots[1].EndTime);
    }
} 