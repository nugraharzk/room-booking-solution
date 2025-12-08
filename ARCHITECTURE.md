# Architecture & Design Decisions

## High-Level Architecture
The solution follows **Clean Architecture** principles, separating concerns into concentric layers with dependency rules pointing inwards.

### Layers
1.  **Domain (`RoomBooking.Domain`)**:
    -   **Responsibility**: Enterprise business rules, Entities, Value Objects.
    -   **Dependencies**: None. Pure C#.
    -   **Key Components**: `User`, `Room`, `Booking` entities.

2.  **Application (`RoomBooking.Application`)**:
    -   **Responsibility**: Application business rules, Use Cases.
    -   **Dependencies**: `Domain`.
    -   **Key Components**:
        -   **CQRS**: Implemented via **MediatR**. Commands (write) and Queries (read) are handled separately.
        -   **Interfaces**: `IUnitOfWork`, `IBookingRepository`, etc.

3.  **Infrastructure (`RoomBooking.Infrastructure`)**:
    -   **Responsibility**: Interface adapters, external frameworks.
    -   **Dependencies**: `Application`.
    -   **Key Components**:
        -   **Persistence**: EF Core with PostgreSQL (`Npgsql`).
        -   **Auth**: JWT generation and parsing.

4.  **API (`RoomBooking.API`)**:
    -   **Responsibility**: Entry point, HTTP Listeners, Controllers.
    -   **Dependencies**: `Application`, `Infrastructure`.
    -   **Key Components**: ASP.NET Core Controllers, Swagger, Dependency Injection setup.

## Key Design Decisions

### 1. CQRS with MediatR
**Why?**
-   Separates read and write workloads, allowing them to scale independently.
-   Decouples Controllers from business logic (Controllers became "thin").
-   Enables easy addition of cross-cutting concerns (logging, validation) via Pipeline Behaviors.

### 2. Rich Domain Model
**Why?**
-   Business logic (e.g., checking if a booking can be confirmed) resides in the Entity (`Booking.Confirm()`), not in services.
-   Ensures invariants are always protected.

### 3. PostgreSQL
**Why?**
-   Robust, open-source relational database.
-   Chosen for its reliability and excellent EF Core support via `Npgsql`.

### 4. Authentication (JWT)
**Why?**
-   Stateless authentication suitable for REST APIs.
-   Role-based claims allow granular authorization without database lookups on every request.

### 5. Frontend Architecture
-   **Vite**: For fast build times.
-   **Tailwind CSS**: For utility-first styling and rapid UI development.
-   **Shadcn UI**: For accessible, unstyled component primitives (Radix UI) on top of Tailwind.
-   **Context API**: Used for lightweight global state (AuthContext).

## Future Considerations
-   **Caching**: Redis could be added to `Infrastructure` to cache Query responses.
-   **Real-time**: SignalR for standardized updates (e.g. new booking notifications).
