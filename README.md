# RoomBooking Solution

A complete full-stack solution for internal room booking, featuring a .NET 9 Web API backend and a React (Vite) frontend.

## Overview

- **Backend**: ASP.NET Core 9 Web API using Clean Architecture, CQRS (MediatR), EF Core, and PostgreSQL.
- **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, and Shadcn UI.
- **Infrastructure**: Containerized with Docker Compose.

## Architecture

- **RoomBooking.Domain**: Entities (`Room`, `Booking`), value objects, and domain logic.
- **RoomBooking.Application**: MediatR commands/queries, DTOs, repository interfaces.
- **RoomBooking.Infrastructure**: EF Core implementation, JWT auth, repositories.
- **RoomBooking.API**: REST API entry point, configured on ports **5200** (HTTP) and **5201** (HTTPS).
- **room-booking-frontend**: Single Page Application (SPA) consuming the API.

## Requirements

- .NET 9 SDK
- Node.js 18+ & npm
- Docker Desktop (for containerized setup)
- Git

## Quick Start (Docker Compose)

1. **Start Services**:
   From the solution root, run:
   ```bash
   docker compose up -d --build
   ```

   - **API**: `http://localhost:5200`
   - **Frontend**: `http://localhost:5173` (proxies to API)
   - **Database**: PostgreSQL on `localhost:5433`

2. **Access the App**:
   Open `http://localhost:5173` in your browser.

   **Default Credentials**:
   - **Admin**: `admin@example.com` / `admin123`
   - **User**: `user@example.com` / `user123`

   *Note: Database is automatically seeded on startup in Development mode.*

## Local Development (Manual Setup)

### Backend

1. **Database**: ensure PostgreSQL is running (or use the docker container `roombooking-db`).
2. **Configuration**: Check `src/RoomBooking.API/appsettings.json`.
3. **Run**:
   ```bash
   cd src/RoomBooking.API
   dotnet run
   ```
   API will listen on `http://localhost:5200` and `https://localhost:5201`.

### Frontend

1. **Install Dependencies**:
   ```bash
   cd room-booking-frontend
   npm install
   ```
2. **Run Dev Server**:
   ```bash
   npm run dev
   ```
   Frontend will run on `http://localhost:5173`.

## Authentication & Authorization

Authentication is JWT-based.

- **Roles**:
  - `Admin`: Full access to Users, Rooms (including inactive), and Global Bookings.
  - `User`: Can book rooms, view/cancel own bookings.
  - `Manager`: Can toggle room active status.
- **Endpoints**:
  - `POST /api/auth/login`: Returns JWT token.

## Key Features implemented

- **Admin Features**:
  - Manage Users (List, Change Role, Deactivate).
  - Manage Rooms (Create, Edit, List All, Toggle Active).
  - Global Booking View.
- **User Features**:
  - Browse Active Rooms.
  - Book Rooms.
  - "My Bookings" dashboard.
- **Tech Highlights**:
  - **Role-Based Access Control (RBAC)** on both Backend (Policies) and Frontend (Protected Routes).
  - **Port Configuration**: Backend moved to **5200/5201** to avoid macOS AirPlay conflict on port 5000.
  - **React StrictMode Disabled**: To prevent double-invocation of API calls during development.

## Docker Notes

- `docker-compose.yml` maps:
  - API: `5050:5050` (Internal) -> Mapped to Host `5200` if running via dotnet run, or `5050` if strictly docker.
  - DB: `5432` (Internal) -> Host `5433` (to avoid conflicts with local Postgres).

## License

Internal use.