using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using MediatR;
using RoomBooking.Application.Interfaces;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Users
{
    // List Users
    public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, UserDto[]>
    {
        private readonly IUnitOfWork _uow;

        public ListUsersQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<UserDto[]> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _uow.Users.ListAllAsync(cancellationToken);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToArray();
        }
    }

    // Update User Role
    public sealed class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, UserDto>
    {
        private readonly IUnitOfWork _uow;

        public UpdateUserRoleCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<UserDto> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
            }

            user.UpdateRole(request.NewRole);
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);

            return MapToDto(user);
        }

        private static UserDto MapToDto(User u)
        {
            return new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            };
        }
    }

    // Toggle User Active
    public sealed class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand, UserDto>
    {
        private readonly IUnitOfWork _uow;

        public ToggleUserActiveCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<UserDto> Handle(ToggleUserActiveCommand request, CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
            }

            user.ToggleActive(request.IsActive);
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);

            return MapToDto(user);
        }

        private static UserDto MapToDto(User u)
        {
            return new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            };
        }
    }

    // Create User
    public sealed class CreateUserHandler(IUnitOfWork uow) : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email '{request.Email}' already exists.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = User.Create(
                request.Email,
                passwordHash,
                request.FirstName,
                request.LastName,
                request.Role
            );

            await _uow.Users.AddAsync(user, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }

    // Delete User
    public sealed class DeleteUserHandler(IUnitOfWork uow) : IRequestHandler<DeleteUserCommand, Unit>
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _uow.Users.GetByIdAsync(request.UserId, cancellationToken) ?? throw new KeyNotFoundException($"User with ID {request.UserId} not found.");

            // Optional: Check rules like "Cannot delete yourself" or "Cannot delete admin if last one"
            // For now, allow deletion.

            _uow.Users.Remove(user);
            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
