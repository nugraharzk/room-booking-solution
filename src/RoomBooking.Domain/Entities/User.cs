using System;

namespace RoomBooking.Domain.Entities
{
    public sealed class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        private User() { }

        public User(Guid id, string email, string passwordHash, string firstName, string lastName, string role)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.", nameof(passwordHash));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Email = email.Trim().ToLowerInvariant();
            PasswordHash = passwordHash;
            FirstName = firstName?.Trim() ?? string.Empty;
            LastName = lastName?.Trim() ?? string.Empty;
            Role = role;
            IsActive = true;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public static User Create(string email, string passwordHash, string firstName, string lastName, string role)
            => new User(Guid.NewGuid(), email, passwordHash, firstName, lastName, role);

        public void UpdateRole(string newRole)
        {
            if (string.IsNullOrWhiteSpace(newRole)) throw new ArgumentException("Role cannot be empty.", nameof(newRole));
            Role = newRole;
        }

        public void ToggleActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
