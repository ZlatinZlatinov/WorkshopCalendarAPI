using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CalendarAPI.Services;
using CalendarAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarAPI.Tests;

public class FreeSlotsServiceTests
{
    private readonly FreeSlotsService _service;
    private readonly ApplicationDbContext _context;

    public FreeSlotsServiceTests()
    {
        _service = new FreeSlotsService();
        _context = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>());
    }

    [Fact]
    public void FindFreeSlots_WithNoEvents_ReturnsAllSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1 };

        // Act
        var slots = _service.FindFreeSlots(new List<Event>(), startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Equal(2, slotsList.Count);
        Assert.Equal(startDate, slotsList[0].StartTime);
        Assert.Equal(startDate.Add(duration), slotsList[0].EndTime);
        Assert.Equal(startDate.AddMinutes(30), slotsList[1].StartTime);
        Assert.Equal(startDate.AddMinutes(60), slotsList[1].EndTime);
    }

    [Fact]
    public void FindFreeSlots_WithOverlappingEvent_ExcludesOverlappingSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1 };

        var existingEvents = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 9, 30, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 10, 30, 0, DateTimeKind.Utc),
                IsCancelled = false,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 1 }
                }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(existingEvents, startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Equal(2, slotsList.Count);
        Assert.Equal(startDate, slotsList[0].StartTime);
        Assert.Equal(startDate.Add(duration), slotsList[0].EndTime);
        Assert.Equal(endDate.AddMinutes(-30), slotsList[1].StartTime);
        Assert.Equal(endDate, slotsList[1].EndTime);
    }

    [Fact]
    public void FindFreeSlots_WithCancelledEvent_IncludesSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1 };

        var existingEvents = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                IsCancelled = true,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 1 }
                }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(existingEvents, startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Equal(2, slotsList.Count);
    }

    [Fact]
    public void FindFreeSlots_WithNonParticipantEvent_IncludesSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1 };

        var existingEvents = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                IsCancelled = false,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 2 }
                }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(existingEvents, startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Equal(2, slotsList.Count);
    }

    [Fact]
    public void FindFreeSlots_WithMultipleParticipants_ConsidersAllParticipants()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1, 2 };

        var existingEvents = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 9, 30, 0, DateTimeKind.Utc),
                IsCancelled = false,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 1 }
                }
            },
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 9, 30, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                IsCancelled = false,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 2 }
                }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(existingEvents, startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Empty(slotsList);
    }

    [Fact]
    public async Task FindFreeSlots_WithDifferentDuration_ReturnsAppropriateSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);
        var participantIds = new List<int> { 1 };

        var existingEvent = new Event
        {
            Title = "Existing Event",
            Description = "Test Description",
            StartTime = startDate.AddHours(1),
            EndTime = startDate.AddHours(2),
            IsCancelled = false
        };
        await _context.Events.AddAsync(existingEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindFreeSlots(startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = result.ToList();
        Assert.Single(slotsList);
        Assert.Equal(startDate.ToUniversalTime(), slotsList[0].StartTime.ToUniversalTime());
        Assert.Equal(startDate.Add(duration).ToUniversalTime(), slotsList[0].EndTime.ToUniversalTime());
    }

    [Fact]
    public void FindFreeSlots_WithEventOutsideRange_ReturnsAllSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromMinutes(30);
        var participantIds = new List<int> { 1 };

        var existingEvents = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 3, 1, 8, 30, 0, DateTimeKind.Utc),
                IsCancelled = false,
                Participants = new List<EventParticipant>
                {
                    new EventParticipant { UserId = 1 }
                }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(existingEvents, startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = slots.ToList();
        Assert.Equal(2, slotsList.Count);
    }

    [Fact]
    public async Task FindFreeSlots_WithMultipleOverlappingEvents_ExcludesAllOverlappingSlots()
    {
        // Arrange
        var startDate = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        var duration = TimeSpan.FromHours(1);
        var participantIds = new List<int> { 1 };

        var events = new List<Event>
        {
            new Event
            {
                Title = "Event 1",
                Description = "Test Description 1",
                StartTime = startDate,
                EndTime = startDate.AddHours(1),
                IsCancelled = false
            },
            new Event
            {
                Title = "Event 2",
                Description = "Test Description 2",
                StartTime = startDate.AddHours(1),
                EndTime = startDate.AddHours(2),
                IsCancelled = false
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.FindFreeSlots(startDate, endDate, duration, participantIds);

        // Assert
        var slotsList = result.ToList();
        Assert.Empty(slotsList);
    }
}