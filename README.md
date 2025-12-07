# RoomBooking API

A clean, production-ready ASP.NET Core Web API for internal room booking with JWT authentication, authorization policies, CQRS via MediatR, and EF Core using PostgreSQL. Containerized with Docker Compose.

## Architecture

- `RoomBooking.Domain`: Entities (`Room`, `Booking`), value objects (`TimeRange`) with domain invariants.
- `RoomBooking.Application`: MediatR commands/queries, DTOs, repository interfaces, `IUnitOfWork`.
- `RoomBooking.Infrastructure`: EF Core (`AppDbContext`), repository implementations, JWT & authorization setup.
- `RoomBooking.API`: Program wiring, controllers, CORS, OpenAPI document.

## Requirements

- .NET SDK (target `net10.0`)
- Docker Desktop (for containerized setup)
- Git (repository already initialized)

## Quick start (Docker Compose)

1. From the solution root (`KalbeTrials\RoomBooking`), build and start services:

   ```
   docker compose up -d --build
   ```

   Services:
   - `db`: PostgreSQL 16 listening on `localhost:5432`
   - `api`: RoomBooking API listening on `http://localhost:5050`

2. Apply EF Core migrations against the running PostgreSQL:

   ```
   dotnet ef database update ^
     -p RoomBooking/src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj ^
     -s RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj ^
     --connection "Host=localhost;Port=5432;Database=RoomBookingDb;Username=postgres;Password=postgres;Include Error Detail=true"
   ```

   Notes:
   - Migrations are generated from `RoomBooking.Infrastructure` and run with `RoomBooking.API` as the startup project.
   - You can also let the containerized API connect first; the database will be empty until you migrate.

3. Test the API:

   - Health check:

     ```
     curl http://localhost:5050/health
     ```

   - OpenAPI (Development only):

     ```
     http://localhost:5050/openapi/v1.json
     ```

## Local development (without Docker)

1. Run PostgreSQL locally or keep Docker `db` running:

   - If using Docker `db`, the connection string in development can point to `localhost:5432`.

2. Configure `RoomBooking/src/RoomBooking.API/appsettings.json`:

   ```
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=RoomBookingDb;Username=postgres;Password=postgres;Include Error Detail=true"
   },
   "Jwt": {
     "Issuer": "RoomBookingAuthServer",
     "Audience": "RoomBookingAPI",
     "Secret": "<set-a-strong-secret>",
     "ValidateIssuer": true,
     "ValidateAudience": true,
     "ValidateLifetime": true,
     "ValidateIssuerSigningKey": true,
     "ClockSkewInSeconds": 60,
     "RequireHttpsMetadata": false,
     "NameClaimType": "name",
     "RoleClaimType": "role"
   }
   ```

3. Build and run:

   ```
   dotnet build RoomBooking.sln
   dotnet run -p RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj
   ```

   - API will listen on `http://localhost:5050` (configurable in `Dockerfile` and environment).
   - Run migrations (if not applied yet):

     ```
     dotnet ef database update ^
       -p RoomBooking/src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj ^
       -s RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj
     ```

## Authentication and Authorization

- JWT Bearer tokens required for most endpoints
- Claims:
  - `sub` or `nameidentifier`: must be a GUID (used as `CreatedByUserId`)
  - `role`: one of `Admin`, `Manager`, `Employee` (used by role policies)
  - `scope` or `scp`: space-delimited list including `bookings.read`/`bookings.write`
- Policies enforced:
  - `Bookings.Read`: requires scope `bookings.read`
  - `Bookings.Write`: requires scope `bookings.write`
  - `RequireManager`: requires role `Manager`

Send tokens via the `Authorization` header:
```
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

- Rooms (`api/rooms`):
  - `POST /api/rooms` (Bookings.Write) — create room
  - `GET /api/rooms/{id}` (Bookings.Read) — get by id
  - `GET /api/rooms/by-name/{name}` (Bookings.Read) — get by unique name
  - `GET /api/rooms` (Bookings.Read) — list active rooms
  - `PUT /api/rooms/{id}` (Bookings.Write) — update name/capacity/location
  - `PATCH /api/rooms/{id}/active` (RequireManager) — toggle active

- Bookings (`api/bookings`):
  - `POST /api/bookings` (Bookings.Write) — create booking; derives `CreatedByUserId` from token
  - `GET /api/bookings/{id}` (Bookings.Read) — get booking
  - `GET /api/bookings/room/{roomId}?from=...&to=...` (Bookings.Read) — list in window
  - `GET /api/bookings/availability?roomId=...&start=...&end=...` (Bookings.Read) — check availability
  - `POST /api/bookings/{id}/confirm` (Bookings.Write)
  - `POST /api/bookings/{id}/cancel` (Bookings.Write)
  - `POST /api/bookings/{id}/complete` (Bookings.Write)
  - `PATCH /api/bookings/{id}/reschedule` (Bookings.Write)

- Health:
  - `GET /health` — returns `status: ok` and timestamp

## Docker details

- `RoomBooking/src/RoomBooking.API/Dockerfile`
  - Multi-stage build
  - Runs as non-root user `appuser`
  - Exposes port `5050`
  - Sets `ConnectionStrings__DefaultConnection` for container networking with service `db`

- `docker-compose.yml`
  - `db` service: Postgres 16 with default credentials (dev-only)
  - `api` service: builds API and depends on `db` becoming healthy
  - Maps `5050:5050` for the API container
  - Environment variables override `appsettings.json` via double-underscore (`__`) notation

- `.dockerignore`
  - Excludes build artifacts and unnecessary files from Docker context

## Git

A Git repository is already initialized in `KalbeTrials\RoomBooking`.

Typical first push:
```
git add .
git commit -m "Initial RoomBooking API with PostgreSQL & Docker"
git branch -M main
git remote add origin <your-remote-url>
git push -u origin main
```

Use the included `.gitignore` to keep build outputs, logs, and secrets out of version control.

## Migrations and Provider Notes

- The solution uses `Npgsql.EntityFrameworkCore.PostgreSQL` for EF Core.
- If you previously generated migrations for SQL Server, regenerate with Npgsql:
  ```
  dotnet ef migrations remove ^
    -p RoomBooking/src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj ^
    -s RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj

  dotnet ef migrations add InitialCreate ^
    -p RoomBooking/src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj ^
    -s RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj
  ```
- Always ensure your connection string points to PostgreSQL when applying migrations.

## Troubleshooting

- Connection issues:
  - Verify `ConnectionStrings__DefaultConnection` in compose or `appsettings.json`.
  - Confirm Postgres is healthy: `docker logs roombooking-db`.

- Unauthorized:
  - Ensure JWT has valid `iss`/`aud` matching `appsettings.json`, a GUID `sub`/`nameidentifier`, appropriate `role`, and `scope` claims.

- EF tooling:
  - Ensure `Microsoft.EntityFrameworkCore.Design` is installed in the startup project (`RoomBooking.API`).

## License

Internal use. Add a license file if required by your organization.