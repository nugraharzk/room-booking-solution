using System;
using RoomBooking.Domain.Entities;
using Xunit;

namespace RoomBooking.Domain.Tests
{
    public class UserTests
    {
        [Fact]
        public void Create_WithValidData_ReturnsUser()
        {
            var email = "test@example.com";
            var passwordHash = "hash123";
            var firstName = "John";
            var lastName = "Doe";
            var role = "User";

            var user = User.Create(email, passwordHash, firstName, lastName, role);

            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
            Assert.Equal(passwordHash, user.PasswordHash);
            Assert.Equal(firstName, user.FirstName);
            Assert.Equal(lastName, user.LastName);
            Assert.Equal(role, user.Role);
            Assert.True(user.IsActive);
        }

        [Fact]
        public void Create_WithMissingRequiredFields_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => User.Create("", "hash", "First", "Last", "Role"));
            Assert.Throws<ArgumentException>(() => User.Create("email", "", "First", "Last", "Role"));
            Assert.Throws<ArgumentException>(() => User.Create("email", "hash", "First", "Last", ""));
        }

        [Fact]
        public void UpdateRole_UpdatesRole()
        {
            var user = User.Create("test@example.com", "hash", "John", "Doe", "User");
            
            user.UpdateRole("Admin");

            Assert.Equal("Admin", user.Role);
        }

        [Fact]
        public void UpdateRole_WithEmptyRole_ThrowsArgumentException()
        {
            var user = User.Create("test@example.com", "hash", "John", "Doe", "User");
            
            Assert.Throws<ArgumentException>(() => user.UpdateRole(""));
        }

        [Fact]
        public void ToggleActive_UpdatesIsActive()
        {
            var user = User.Create("test@example.com", "hash", "John", "Doe", "User");
            
            user.ToggleActive(false);
            Assert.False(user.IsActive);

            user.ToggleActive(true);
            Assert.True(user.IsActive);
        }
    }
}
