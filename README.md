# Atlas

Atlas is an ASP.NET Core MVC social web platform for creating, finding, and discussing geographic places through an interactive map.

## Tech Stack

- .NET 10
- C#
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- Razor Views
- Bootstrap
- Leaflet
- OpenStreetMap
- xUnit
- Testcontainers

## Features

- Registration, login, and logout
- Admin and User roles
- User profiles
- Geographic places
- Global Map and My Map
- Posts with image and video media
- Comments and replies
- Likes and dislikes
- Place ratings
- Follow and unfollow
- Personalized feed
- Search and filtering
- Custom categories
- Notifications
- Reputation and explorer levels
- Password reset by email
- Antiforgery protection
- HTTP request logging
- Automated integration and business tests

## Project Structure

- `Project2106/` — the ASP.NET Core MVC application.
  - `Controllers/` — MVC request handling and authorization checks.
  - `Models/` — application and Entity Framework entities.
  - `ViewModels/` — models used for form input and view rendering.
  - `Views/` — Razor views and shared partials.
  - `Data/` — the EF Core database context.
  - `Services/` — Identity bootstrap and password-reset email services.
  - `Middleware/` — HTTP request logging middleware.
  - `Migrations/` — EF Core migrations and the model snapshot.
  - `wwwroot/` — CSS, JavaScript, images, and client libraries.
- `PROJECT2106.Tests/` — xUnit tests using PostgreSQL through Testcontainers.

## Requirements

- .NET 10 SDK
- PostgreSQL
- A Docker-compatible environment for integration tests through Testcontainers

## Configuration

Connection strings, passwords, and other sensitive settings must not be committed to source control. Configure them with environment variables or .NET user secrets.

Example with user secrets:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=atlas;Username=YOUR_USER;Password=YOUR_PASSWORD" \
  --project Project2106/PROJECT2106.csproj
```

Equivalent environment variable:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=atlas;Username=YOUR_USER;Password=YOUR_PASSWORD'
```

Optional administrator bootstrap settings use the `BootstrapAdmin` section. Password-reset email settings use the `Smtp` section. Store their sensitive values through the same mechanisms.

## Database

Apply the included EF Core migrations to the configured PostgreSQL database:

```bash
dotnet ef database update --project Project2106/PROJECT2106.csproj
```

Create a new migration only after an intentional model change. Existing migrations represent the project schema history.

## Run

From the repository root:

```bash
dotnet restore Project2106/PROJECT2106.csproj
dotnet build Project2106/PROJECT2106.csproj
dotnet run --project Project2106/PROJECT2106.csproj
```

## Tests

The integration tests start an isolated PostgreSQL container, so Docker must be available.

```bash
dotnet test PROJECT2106.Tests/PROJECT2106.Tests.csproj
```

## Security Notes

- Authentication, password management, and roles use ASP.NET Core Identity.
- Unsafe MVC requests are protected by antiforgery validation.
- Role and ownership checks protect restricted operations.
- Secrets are supplied through environment variables or .NET user secrets.
- Upload handlers enforce the application's current file-count, size, extension, and MIME-type rules and generate server-side filenames.
