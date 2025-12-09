using System;
using MediatR;

namespace RoomBooking.Application.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    // Queries
    public record ListUsersQuery : IRequest<UserDto[]>;
    
    // Commands
    public record UpdateUserRoleCommand(Guid UserId, string NewRole) : IRequest<UserDto>;
    public record ToggleUserActiveCommand(Guid UserId, bool IsActive) : IRequest<UserDto>;

    public record CreateUserCommand(
        string Email, 
        string Password, 
        string FirstName, 
        string LastName, 
        string Role
    ) : IRequest<UserDto>;

    public record DeleteUserCommand(Guid UserId) : IRequest<Unit>;
}
