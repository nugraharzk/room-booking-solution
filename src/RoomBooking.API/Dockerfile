# syntax=docker/dockerfile:1

# ---------------------------------------------------------
# Build stage
# ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy entire repository (ensure build context is solution root)
COPY . .

# Restore and publish API project
RUN dotnet restore RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj \
  && dotnet publish RoomBooking/src/RoomBooking.API/RoomBooking.API.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# ---------------------------------------------------------
# Runtime stage
# ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Create non-root app user for better security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

WORKDIR /app

# Copy published artifacts
COPY --from=build /app/publish ./

# Environment & ports
ENV ASPNETCORE_URLS=http://+:5050
# Default Postgres connection string; override via environment in deployment
ENV ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=RoomBookingDb;Username=postgres;Password=postgres;Include Error Detail=true"

EXPOSE 5050

# Run the API
ENTRYPOINT ["dotnet", "RoomBooking.API.dll"]
