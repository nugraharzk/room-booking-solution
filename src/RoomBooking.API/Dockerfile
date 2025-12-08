# syntax=docker/dockerfile:1

# ---------------------------------------------------------
# Build stage
# ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy entire repository (ensure build context is solution root)
COPY . .

# Restore and publish API project
RUN dotnet restore src/RoomBooking.API/RoomBooking.API.csproj \
  && dotnet publish src/RoomBooking.API/RoomBooking.API.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ---------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Create non-root app user for better security
RUN useradd -m -s /usr/sbin/nologin appuser
USER appuser

WORKDIR /app

# Copy published artifacts
COPY --from=build /app/publish ./

# Environment & ports
ENV ASPNETCORE_URLS=http://+:5050
# Default Postgres connection string; override via environment in deployment
ENV ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=roombooking;Username=postgres;Password=postgres123;Include Error Detail=true"

EXPOSE 5050

# Run the API
ENTRYPOINT ["dotnet", "RoomBooking.API.dll"]
