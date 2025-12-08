using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
}
