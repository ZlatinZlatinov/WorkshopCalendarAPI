using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using CalendarAPI.Data;
using CalendarAPI.Models;
using CalendarAPI.Services;

namespace CalendarAPI.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Set up configuration
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"Jwt:Key", "your-super-secret-key-with-at-least-32-characters"},
            {"Jwt:Issuer", "CalendarAPI"},
            {"Jwt:Audience", "CalendarClient"}
        });
        _configuration = configurationBuilder.Build();

        _authService = new AuthService(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Registration successful", result.Message);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(request.Name, result.User.Name);
        Assert.Equal(request.Email, result.User.Email);
        Assert.NotEmpty(result.User.Id.ToString());
    }

    [Theory]
    [InlineData("", "test@example.com", "password123", "Name is required")]
    [InlineData("T", "test@example.com", "password123", "Name must be at least 2 characters")]
    [InlineData("Test User", "", "password123", "Email is required")]
    [InlineData("Test User", "invalid-email", "password123", "Invalid email format")]
    [InlineData("Test User", "test@example.com", "", "Password is required")]
    [InlineData("Test User", "test@example.com", "12345", "Password must be at least 6 characters")]
    public async Task RegisterAsync_WithInvalidData_ShouldFail(string name, string email, string password, string expectedMessage)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = name,
            Email = email,
            Password = password
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldFail()
    {
        // Arrange
        var existingUser = new User
        {
            Name = "Existing User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User with this email already exists", result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var password = "password123";
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = password, // In a real scenario, this would be hashed
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(user.Name, result.User.Name);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.Id, result.User.Id);
    }

    [Theory]
    [InlineData("", "password123", "Email is required")]
    [InlineData("invalid-email", "password123", "Invalid email format")]
    [InlineData("test@example.com", "", "Password is required")]
    public async Task LoginAsync_WithInvalidData_ShouldFail(string email, string password, string expectedMessage)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "correctpassword", // In a real scenario, this would be hashed
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Token);
        Assert.Null(result.User);
    }
} 