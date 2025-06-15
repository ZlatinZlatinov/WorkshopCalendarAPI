using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CalendarAPI.Data;
using CalendarAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace CalendarAPI.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(request.Name))
        {
            return new AuthResponse { Success = false, Message = "Name is required" };
        }
        if (request.Name.Length < 2)
        {
            return new AuthResponse { Success = false, Message = "Name must be at least 2 characters" };
        }
        if (string.IsNullOrEmpty(request.Email))
        {
            return new AuthResponse { Success = false, Message = "Email is required" };
        }
        if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            return new AuthResponse { Success = false, Message = "Invalid email format" };
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            return new AuthResponse { Success = false, Message = "Password is required" };
        }
        if (request.Password.Length < 6)
        {
            return new AuthResponse { Success = false, Message = "Password must be at least 6 characters" };
        }

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "User with this email already exists"
            };
        }

        // Create new user
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Validate input
        if (string.IsNullOrEmpty(request.Email))
        {
            return new AuthResponse { Success = false, Message = "Email is required" };
        }
        if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            return new AuthResponse { Success = false, Message = "Invalid email format" };
        }
        if (string.IsNullOrEmpty(request.Password))
        {
            return new AuthResponse { Success = false, Message = "Password is required" };
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Generate token
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == passwordHash;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 