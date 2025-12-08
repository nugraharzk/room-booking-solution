using System;
using System.Linq;
using RoomBooking.Domain.Entities;
using RoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RoomBooking.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Apply any pending migrations
            context.Database.Migrate();

            SeedRooms(context);
            SeedUsers(context);

            context.SaveChanges();
        }

        private static void SeedRooms(AppDbContext context)
        {
            if (context.Rooms.Any()) return;

            var rooms = new Room[]
            {
                Room.Create("Conference Room A", 10, "1st Floor, North Wing"),
                Room.Create("Conference Room B", 12, "1st Floor, South Wing"),
                Room.Create("Small Meeting Room", 4, "2nd Floor"),
                Room.Create("Board Room", 20, "Top Floor")
            };

            context.Rooms.AddRange(rooms);
        }

        private static void SeedUsers(AppDbContext context)
        {
            if (!context.Users.Any(u => u.Email == "admin@example.com"))
            {
                var admin = User.Create(
                    "admin@example.com",
                    BCrypt.Net.BCrypt.HashPassword("admin123"),
                    "Admin",
                    "User",
                    RoomBooking.Infrastructure.Auth.Roles.Admin
                );
                context.Users.Add(admin);
            }

            if (!context.Users.Any(u => u.Email == "manager@example.com"))
            {
                var manager = User.Create(
                    "manager@example.com",
                    BCrypt.Net.BCrypt.HashPassword("manager123"),
                    "Manager",
                    "User",
                    RoomBooking.Infrastructure.Auth.Roles.Manager
                );
                context.Users.Add(manager);
            }

            if (!context.Users.Any(u => u.Email == "user@example.com"))
            {
                var user = User.Create(
                    "user@example.com",
                    BCrypt.Net.BCrypt.HashPassword("user123"),
                    "Normal",
                    "User",
                    RoomBooking.Infrastructure.Auth.Roles.User
                );
                context.Users.Add(user);
            }
        }
    }
}
