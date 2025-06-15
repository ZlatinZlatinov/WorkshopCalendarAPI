using System;
using System.Collections.Generic;
using System.Linq;
using WorkshopCalendarAPI.Models;
using WorkshopCalendarAPI.Services;
using Xunit;

namespace WorkshopCalendarAPI.Tests;

public class FreeSlotsServiceTests
{
    private readonly FreeSlotsService _service = new FreeSlotsService();

    [Fact]
    public void FindsFreeSlot_WhenNoEvents()
    {
        // Arrange
        var events = new List<Event>();
        var start = new DateTime(2024, 6, 15, 9, 0, 0);
        var end = new DateTime(2024, 6, 15, 17, 0, 0);
        var duration = TimeSpan.FromHours(1);
        var participants = new List<int> { 1, 2 };

        // Act
        var slots = _service.FindFreeSlots(events, start, end, duration, participants).ToList();

        // Assert
        Assert.NotEmpty(slots);
        Assert.Contains(slots, s => s.StartTime == start && s.EndTime == start.Add(duration));
    }

    [Fact]
    public void NoFreeSlot_WhenEventsCoverAllTime()
    {
        // Arrange
        var start = new DateTime(2024, 6, 15, 9, 0, 0);
        var end = new DateTime(2024, 6, 15, 17, 0, 0);
        var duration = TimeSpan.FromHours(1);
        var participants = new List<int> { 1 };
        var events = new List<Event>
        {
            new Event
            {
                StartTime = start,
                EndTime = end,
                IsCancelled = false,
                Participants = new List<EventParticipant> { new EventParticipant { UserId = 1 } }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(events, start, end, duration, participants).ToList();

        // Assert
        Assert.Empty(slots);
    }

    [Fact]
    public void FindsFreeSlot_BetweenEvents()
    {
        // Arrange
        var start = new DateTime(2024, 6, 15, 9, 0, 0);
        var end = new DateTime(2024, 6, 15, 17, 0, 0);
        var duration = TimeSpan.FromHours(1);
        var participants = new List<int> { 1 };
        var events = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 6, 15, 9, 0, 0),
                EndTime = new DateTime(2024, 6, 15, 10, 0, 0),
                IsCancelled = false,
                Participants = new List<EventParticipant> { new EventParticipant { UserId = 1 } }
            },
            new Event
            {
                StartTime = new DateTime(2024, 6, 15, 11, 0, 0),
                EndTime = new DateTime(2024, 6, 15, 12, 0, 0),
                IsCancelled = false,
                Participants = new List<EventParticipant> { new EventParticipant { UserId = 1 } }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(events, start, end, duration, participants).ToList();

        // Assert
        Assert.Contains(slots, s => s.StartTime == new DateTime(2024, 6, 15, 10, 0, 0));
    }

    [Fact]
    public void IgnoresCancelledEvents()
    {
        // Arrange
        var start = new DateTime(2024, 6, 15, 9, 0, 0);
        var end = new DateTime(2024, 6, 15, 17, 0, 0);
        var duration = TimeSpan.FromHours(1);
        var participants = new List<int> { 1 };
        var events = new List<Event>
        {
            new Event
            {
                StartTime = new DateTime(2024, 6, 15, 9, 0, 0),
                EndTime = new DateTime(2024, 6, 15, 10, 0, 0),
                IsCancelled = true,
                Participants = new List<EventParticipant> { new EventParticipant { UserId = 1 } }
            }
        };

        // Act
        var slots = _service.FindFreeSlots(events, start, end, duration, participants).ToList();

        // Assert
        Assert.NotEmpty(slots);
    }
} 