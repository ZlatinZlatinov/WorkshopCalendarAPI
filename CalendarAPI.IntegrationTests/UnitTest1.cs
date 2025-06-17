using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using CalendarAPI.Models;
using System.Collections.Generic;

namespace CalendarAPI.IntegrationTests
{
    public class UsersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public UsersEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private record AuthResponse(bool Success, string? Message, string? Token, UserDto? User);
        private record RegisterRequest(string Name, string Email, string Password);
        private record LoginRequest(string Email, string Password);
        private record UserDto(int Id, string Name, string Email);

        private async Task<string> RegisterAndLoginAsync(HttpClient client, string email, string password, string name)
        {
            // Register
            var register = new RegisterRequest(name, email, password);
            var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", register);
            regResp.EnsureSuccessStatusCode();
            // Login
            var login = new LoginRequest(email, password);
            var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", login);
            loginResp.EnsureSuccessStatusCode();
            var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
            auth.Should().NotBeNull();
            auth!.Success.Should().BeTrue();
            auth.Token.Should().NotBeNullOrEmpty();
            return auth.Token!;
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var client = _factory.CreateClient();
            var email = $"testuser_{System.Guid.NewGuid()}@example.com";
            var password = "TestPassword123!";
            var name = "Test User";
            var token = await RegisterAndLoginAsync(client, email, password, name);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task GetAllUsers_ReturnsSuccessAndJson()
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task CreateUser_ThenGetUserById_Works()
        {
            var client = await GetAuthenticatedClientAsync();
            var newUser = new User
            {
                Name = "Test User",
                Email = $"testuser_{System.Guid.NewGuid()}@example.com",
                PasswordHash = "hashedpassword"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/users", newUser);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
            createdUser.Should().NotBeNull();
            createdUser!.Id.Should().BeGreaterThan(0);
            var getResponse = await client.GetAsync($"/api/v1/users/{createdUser.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetchedUser = await getResponse.Content.ReadFromJsonAsync<User>();
            fetchedUser.Should().NotBeNull();
            fetchedUser!.Email.Should().Be(newUser.Email);
        }

        [Fact]
        public async Task UpdateUser_Works()
        {
            var client = await GetAuthenticatedClientAsync();
            var newUser = new User
            {
                Name = "Update User",
                Email = $"updateuser_{System.Guid.NewGuid()}@example.com",
                PasswordHash = "hashedpassword"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/users", newUser);
            var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
            createdUser!.Name = "Updated Name";
            var updateResponse = await client.PutAsJsonAsync($"/api/v1/users/{createdUser.Id}", createdUser);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var getResponse = await client.GetAsync($"/api/v1/users/{createdUser.Id}");
            var updatedUser = await getResponse.Content.ReadFromJsonAsync<User>();
            updatedUser!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task GetUserEvents_ReturnsEmptyListForNewUser()
        {
            var client = await GetAuthenticatedClientAsync();
            var newUser = new User
            {
                Name = "Eventless User",
                Email = $"eventless_{System.Guid.NewGuid()}@example.com",
                PasswordHash = "hashedpassword"
            };
            var createResponse = await client.PostAsJsonAsync("/api/v1/users", newUser);
            var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
            var eventsResponse = await client.GetAsync($"/api/v1/users/{createdUser!.Id}/events");
            eventsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var events = await eventsResponse.Content.ReadFromJsonAsync<List<object>>();
            events.Should().NotBeNull();
            events!.Should().BeEmpty();
        }
    }
}
