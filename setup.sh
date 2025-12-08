#!/bin/bash

# Setup script for RoomBooking Solution

echo "🚀 Starting Setup..."

# 1. Start Docker Containers
echo "\n🐳 Starting Docker Services..."
docker compose up -d --build

# 2. Waiting for DB
echo "⏳ Waiting for Database to be ready..."
sleep 5

# 3. Apply Migrations
echo "\n📦 Applying Database Migrations..."
dotnet ef database update \
  -p src/RoomBooking.Infrastructure/RoomBooking.Infrastructure.csproj \
  -s src/RoomBooking.API/RoomBooking.API.csproj

echo "\n✅ Setup Complete!"
echo "------------------------------------------------"
echo "API is running at:      http://localhost:5200"
echo "Swagger UI:             https://localhost:5201/swagger"
echo "Default Admin:          admin@example.com / admin123"
echo "------------------------------------------------"
