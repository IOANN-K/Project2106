# PROJECT2106 / Atlas

Atlas is a geo-social network for explorers, researchers, and travelers. Explorers can discover places on an interactive map, publish contributions with media, rate places, follow other explorers, and build a personal map and feed.

## Technology

- .NET 10 and ASP.NET Core MVC
- Entity Framework Core 10
- PostgreSQL
- ASP.NET Core Identity
- Razor Views and Bootstrap
- Leaflet with OpenStreetMap tiles

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 14 or newer (local PostgreSQL 18 is used for the current validation)
- A current desktop browser
- Optional: `dotnet-ef` 10.x if it is not already installed

Install the EF CLI when needed:

```bash
dotnet tool install --global dotnet-ef --version 10.*
```

Docker is not required and this repository does not include a Docker setup.

## Start from a clean clone

```bash
git clone https://github.com/IOANN-K/Project2106.git
cd Project2106
dotnet restore PROJECT2106.csproj
```

Create an empty PostgreSQL database. The command below prompts for the PostgreSQL user's password when required:

```bash
createdb --host localhost --port 5432 --username postgres project2106
```

Store the connection string outside Git with .NET user secrets:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=project2106;Username=postgres;Password=YOUR_LOCAL_PASSWORD" \
  --project PROJECT2106.csproj
```

Apply all migrations and start Atlas:

```bash
dotnet ef database update --project PROJECT2106.csproj
dotnet run --project PROJECT2106.csproj
```

The default launch profile opens `http://localhost:5157`. The HTTPS profile also uses `https://localhost:7004`; run `dotnet dev-certs https --trust` if the local development certificate is not trusted. The process health endpoint is available at `/health`.

Verify PostgreSQL directly when troubleshooting connectivity:

```bash
psql --host localhost --port 5432 --username postgres --dbname project2106 \
  --command "select current_database(), version();"
```

## Configuration

`appsettings.json` contains only safe empty defaults. Local secrets can be supplied with user secrets as above, or with environment variables. ASP.NET Core maps double underscores to configuration section separators:

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=project2106;Username=postgres;Password=...'
```

Do not put database or account passwords in tracked `appsettings*.json` files. Environment variables override JSON configuration; user secrets are loaded in the Development environment.

### Roles and optional administrator bootstrap

On startup, Atlas ensures that the required `Admin` and `User` Identity roles exist. This bootstrap is skipped in the `Testing` environment so the test host can configure its own database.

Administrator creation is disabled by default. To create one account on startup, provide all settings through user secrets or environment variables:

```bash
dotnet user-secrets set "BootstrapAdmin:Enabled" "true" --project PROJECT2106.csproj
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.test" --project PROJECT2106.csproj
dotnet user-secrets set "BootstrapAdmin:UserName" "atlas_admin" --project PROJECT2106.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "YOUR_STRONG_LOCAL_PASSWORD" --project PROJECT2106.csproj
```

Equivalent environment variables are `BootstrapAdmin__Enabled`, `BootstrapAdmin__Email`, `BootstrapAdmin__UserName`, and `BootstrapAdmin__Password`. The password is used only when creating a missing account; existing account data is not overwritten. Disable the option again after bootstrap if it is no longer needed.

Atlas does not seed fake explorers, places, contributions, or demo credentials. Demo-state preparation is intentionally separate from normal startup.

### Password reset email

The Forgot Password flow uses SMTP to deliver ASP.NET Identity reset links. SMTP is intentionally unconfigured by default. Store provider credentials in user secrets:

```bash
dotnet user-secrets set "Smtp:Host" "smtp.example.com" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:Port" "587" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:EnableSsl" "true" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:Username" "YOUR_SMTP_USERNAME" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:FromAddress" "no-reply@example.com" --project PROJECT2106.csproj
dotnet user-secrets set "Smtp:FromName" "Atlas Explorer Network" --project PROJECT2106.csproj
```

For an SMTP relay without authentication, leave both `Smtp:Username` and `Smtp:Password` empty. Equivalent environment variables use the `Smtp__` prefix, for example `Smtp__Host` and `Smtp__Password`.

Forgot Password always shows the same confirmation message whether an account exists or not. Reset tokens are sent only by email and are never rendered or logged. If SMTP delivery fails, the failure is logged server-side without exposing account existence to the requester.

## Entity Framework migrations

List migrations and check their applied state:

```bash
dotnet ef migrations list --project PROJECT2106.csproj
dotnet ef migrations has-pending-model-changes --project PROJECT2106.csproj
```

Create and apply a migration:

```bash
dotnet ef migrations add MigrationName --project PROJECT2106.csproj
dotnet ef database update --project PROJECT2106.csproj
```

For a development-only rollback, first back up any data that matters, then target the preceding migration by its exact name:

```bash
dotnet ef database update PreviousMigrationName --project PROJECT2106.csproj
```

Do not edit migrations that have already been applied to shared databases. A new empty database is fully initialized by `dotnet ef database update`; no tables or roles need to be inserted manually before the first application start.

Two historical migrations, `AddEditedFieldsToComment` and `AddLikes`, are no-ops. Their immediately following migrations (`AddEditedFieldsToComment2` and `AddLike`) contain the intended schema changes. The no-op entries are intentionally retained because they are already part of migration history; removing them would make existing databases inconsistent.

## Local uploads

Development uploads are stored below `wwwroot/uploads/`:

- `avatars/` for profile photos;
- `category-icons/` for custom marker icons;
- `posts/` for contribution images and videos.

The upload handlers create these directories lazily with `Directory.CreateDirectory`, so a clean clone does not need empty folders. Uploaded files are ignored by Git and are local filesystem storage, not durable shared/cloud storage. Back them up separately before replacing a development environment.

Current limits are 5 MB for avatars, 2 MB for category icons, and up to 10 contribution media files (10 MB per image or 50 MB per video). Upload handlers validate file count, size, extension, and MIME type, and generate server-side filenames.

## Build and tests

Build the application from the repository root:

```bash
dotnet restore PROJECT2106.csproj
dotnet build PROJECT2106.csproj --no-restore
```

In the full development workspace, the automated test project is a sibling of this application checkout. From this repository root run:

```bash
dotnet test ../PROJECT2106.Tests/PROJECT2106.Tests.csproj
```

The integration tests use Testcontainers with PostgreSQL, so Docker must be available when those tests are run. The application itself does not require Docker.

## Production notes

- Set `ASPNETCORE_ENVIRONMENT=Production` and provide the connection string through the deployment secret store.
- Apply migrations as an explicit deployment step before starting the new application version.
- Non-development environments use `/Home/Error` and HSTS; stack traces are not rendered to users.
- `/health` reports process health. It is intentionally not a database readiness probe.
- Replace local upload storage with persistent storage, or mount `wwwroot/uploads`, before multi-instance or ephemeral deployment.
- Supply deployment-specific privacy terms before a public launch.
