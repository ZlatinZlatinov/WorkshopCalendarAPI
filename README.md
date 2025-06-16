# Workshop Calendar API

A .NET 9 ASP.NET Core Web API for managing personal calendars with support for multiple users, event management, and finding free time slots among participants.

## Features

- User management (simplified authentication)
- Event CRUD operations
- Participant management for events
- Finding free time slots among multiple users
- RESTful API design
- Swagger/OpenAPI documentation

## Prerequisites

- .NET 9 SDK
- SQL Server (local or remote)
- Visual Studio 2022, VS Code, or JetBrains Rider

## Getting Started

1. Clone the repository:
```bash
git clone <repository-url>
cd CalendarAPI
```

2. Update the connection string in `appsettings.json` to point to your SQL Server instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WorkshopCalendar;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

3. Run the database migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

4. Start the application:
```bash
dotnet run
```

5. Access the Swagger UI at:
- http://localhost:5000/swagger (HTTP)
- https://localhost:5001/swagger (HTTPS)

## API Endpoints

### Events
- `GET /api/v1/events` - Get all events (with optional date filtering)
- `GET /api/v1/events/{id}` - Get a specific event
- `POST /api/v1/events` - Create a new event
- `PUT /api/v1/events/{id}` - Update an event
- `DELETE /api/v1/events/{id}` - Cancel an event
- `GET /api/v1/events/free-slots` - Find free time slots for participants

### Event Participants
- `GET /api/v1/events/{eventId}/participants` - Get all participants for an event
- `POST /api/v1/events/{eventId}/participants` - Add a participant to an event
- `PUT /api/v1/events/{eventId}/participants/{userId}` - Update participant status
- `DELETE /api/v1/events/{eventId}/participants/{userId}` - Remove a participant

### Users
- `GET /api/v1/users` - Get all users
- `GET /api/v1/users/{id}` - Get a specific user
- `POST /api/v1/users` - Create a new user
- `PUT /api/v1/users/{id}` - Update a user
- `GET /api/v1/users/{id}/events` - Get all events for a user

## Data Models

### User
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<EventParticipant> EventParticipants { get; set; }
}
```

### Event
```csharp
public class Event
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsCancelled { get; set; }
    public ICollection<EventParticipant> Participants { get; set; }
}
```

### EventParticipant
```csharp
public class EventParticipant
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Revision { get; set; }
}
```

## Finding Free Slots

The API includes a service for finding available time slots among multiple users. The algorithm:
1. Takes a date range and duration
2. Considers all events for the specified participants
3. Returns all available time slots where all participants are free
4. Uses 30-minute intervals for slot suggestions

Example request:
```
GET /api/v1/events/free-slots?startDate=2024-03-20T09:00:00Z&endDate=2024-03-20T17:00:00Z&durationMinutes=60&participantIds=1&participantIds=2
```

## Development

### Project Structure
```
WorkshopCalendarAPI/
├── CalendarAPI/                    # Main API project
│   ├── Controllers/               # API endpoints
│   │   ├── AuthController.cs      # Authentication endpoints
│   │   ├── EventsController.cs    # Event management endpoints
│   │   └── UsersController.cs     # User management endpoints
│   ├── Data/                      # Data access layer
│   │   └── ApplicationDbContext.cs # Database context
│   ├── Migrations/                # Database migrations
│   │   ├── 20250614170553_InitialCreate.cs
│   │   └── ApplicationDbContextModelSnapshot.cs
│   ├── Models/                    # Data models
│   │   ├── AuthModels.cs         # Authentication models
│   │   ├── Event.cs              # Event model
│   │   ├── EventParticipant.cs   # Event participant model
│   │   └── User.cs               # User model
│   ├── Services/                  # Business logic
│   │   ├── AuthService.cs        # Authentication service
│   │   └── FreeSlotsService.cs   # Free slots calculation service
│   ├── Program.cs                 # Application entry point
│   └── appsettings.json          # Application configuration
│
└── CalendarAPI.Tests/             # Test project
    ├── AuthServiceTests.cs       # Authentication service tests
    ├── EventsControllerTests.cs  # Event controller tests
    ├── FreeSlotsServiceTests.cs  # Free slots service tests
    └── UsersControllerTests.cs   # User controller tests
```

### Key Components

#### Controllers
- `AuthController`: Handles user registration and login
- `EventsController`: Manages calendar events and free slots
- `UsersController`: Handles user management

#### Services
- `AuthService`: Implements authentication logic and JWT token generation
- `FreeSlotsService`: Calculates available time slots for multiple users

#### Models
- `User`: User information and authentication
- `Event`: Calendar event details
- `EventParticipant`: Event participation information
- `AuthModels`: Authentication request/response models

#### Data
- `ApplicationDbContext`: Entity Framework Core database context
- Migrations: Database schema management

## Security Considerations

- Passwords are hashed before storage
- API endpoints use HTTPS
- CORS is configured for development
- Input validation using data annotations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## GitHub Workflow

This project includes a GitHub workflow that automatically builds and runs tests on commit and merge events. The workflow is defined in the `.github/workflows/test.yml` file.

### Workflow Summary:
- **Trigger:** On push to `main` and on pull requests to `main`.
- **Environment:** Runs on the latest Ubuntu runner.
- **Steps:**
  1. **Checkout code:** Fetches the latest code from the repository.
  2. **Setup .NET:** Configures the .NET environment with version 9.0.x.
  3. **Restore dependencies:** Restores the project dependencies.
  4. **Build:** Builds the project without restoring dependencies again.
  5. **Test:** Runs the tests without rebuilding the project.

### Next Steps:
- Ensure that your repository has the `.github/workflows` directory.
- Commit the `test.yml` file to your repository.
- Push the changes to GitHub, and the workflow will automatically run on the specified events. 