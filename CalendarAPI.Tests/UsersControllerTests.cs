using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CalendarAPI.Controllers;
using CalendarAPI.Data;
using CalendarAPI.Models;

namespace CalendarAPI.Tests;

public class UsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _controller = new UsersController(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Name = "User 1",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "User 2",
                Email = "user2@example.com",
                PasswordHash = "hash2",
                CreatedAt = DateTime.UtcNow
            }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = Assert.IsType<ActionResult<IEnumerable<User>>>(result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<User>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
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
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUser(user.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<User>>(result);
        var returnValue = Assert.IsType<User>(okResult.Value);
        Assert.Equal(user.Id, returnValue.Id);
        Assert.Equal(user.Name, returnValue.Name);
        Assert.Equal(user.Email, returnValue.Email);
        Assert.Equal(user.PasswordHash, returnValue.PasswordHash);
        Assert.Equal(user.CreatedAt, returnValue.CreatedAt);
        Assert.Empty(returnValue.EventParticipants);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetUser(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var user = new User
        {
            Name = "New User",
            Email = "new@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _controller.CreateUser(user);

        // Assert
        var createdResult = Assert.IsType<ActionResult<User>>(result);
        var returnValue = Assert.IsType<User>(createdResult.Value);
        Assert.Equal(user.Name, returnValue.Name);
        Assert.Equal(user.Email, returnValue.Email);
        Assert.Equal(user.PasswordHash, returnValue.PasswordHash);
        Assert.Equal(user.CreatedAt, returnValue.CreatedAt);
        Assert.Empty(returnValue.EventParticipants);
    }

    [Theory]
    [InlineData("", "test@example.com", "hash")]
    [InlineData("Test User", "", "hash")]
    [InlineData("Test User", "test@example.com", "")]
    [InlineData("Test User", "invalid-email", "hash")]
    public async Task CreateUser_WithInvalidData_ReturnsBadRequest(string name, string email, string passwordHash)
    {
        // Arrange
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _controller.CreateUser(user);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var user = new User
        {
            Name = "Original User",
            Email = "original@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        user.Name = "Updated User";
        user.Email = "updated@example.com";

        // Act
        var result = await _controller.UpdateUser(user.Id, user);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated User", updatedUser.Name);
        Assert.Equal("updated@example.com", updatedUser.Email);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var user = new User
        {
            Id = 999,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _controller.UpdateUser(999, user);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _controller.UpdateUser(2, user);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task GetUser_WithEventParticipants_ReturnsUserWithParticipants()
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
        var result = await _controller.GetUser(user.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<User>>(result);
        var returnValue = Assert.IsType<User>(okResult.Value);
        Assert.Single(returnValue.EventParticipants);
        var returnedParticipant = returnValue.EventParticipants.First();
        Assert.Equal(@event.Id, returnedParticipant.EventId);
        Assert.Equal(user.Id, returnedParticipant.UserId);
        Assert.True(returnedParticipant.IsRequired);
        Assert.Equal(1, returnedParticipant.Revision);
    }

    [Fact]
    public async Task GetUser_WithMultipleEvents_ReturnsUserWithAllEvents()
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

        var participants = new List<EventParticipant>
        {
            new EventParticipant
            {
                EventId = events[0].Id,
                UserId = user.Id,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow,
                Revision = 1
            },
            new EventParticipant
            {
                EventId = events[1].Id,
                UserId = user.Id,
                IsRequired = false,
                CreatedAt = DateTime.UtcNow,
                Revision = 1
            }
        };
        await _context.EventParticipants.AddRangeAsync(participants);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUser(user.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<User>>(result);
        var returnValue = Assert.IsType<User>(okResult.Value);
        Assert.Equal(2, returnValue.EventParticipants.Count);
        
        var firstParticipant = returnValue.EventParticipants.First(p => p.EventId == events[0].Id);
        Assert.True(firstParticipant.IsRequired);
        
        var secondParticipant = returnValue.EventParticipants.First(p => p.EventId == events[1].Id);
        Assert.False(secondParticipant.IsRequired);
    }
} 