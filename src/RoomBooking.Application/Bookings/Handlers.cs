using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RoomBooking.Application.Interfaces;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Bookings
{
    // Rooms - Command Handlers

    internal sealed class CreateRoomHandler : IRequestHandler<CreateRoomCommand, RoomDto>
    {
        private readonly IUnitOfWork _uow;

        public CreateRoomHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken ct)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Room name is required.", nameof(request.Name));

            if (request.Capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.Capacity), "Capacity must be greater than zero.");

            // Enforce unique name
            if (await _uow.Rooms.ExistsByNameAsync(name, ct))
                throw new InvalidOperationException($"A room with name '{name}' already exists.");

            var room = Room.Create(name, request.Capacity, request.Location);
            await _uow.Rooms.AddAsync(room, ct);
            await _uow.SaveChangesAsync(ct);

            return room.ToDto();
        }
    }

    internal sealed class UpdateRoomDetailsHandler : IRequestHandler<UpdateRoomDetailsCommand, RoomDto>
    {
        private readonly IUnitOfWork _uow;

        public UpdateRoomDetailsHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<RoomDto> Handle(UpdateRoomDetailsCommand request, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(request.RoomId, ct);
            if (room is null)
                throw new KeyNotFoundException($"Room '{request.RoomId}' was not found.");

            var newName = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Room name is required.", nameof(request.Name));

            if (request.Capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(request.Capacity), "Capacity must be greater than zero.");

            // Check name uniqueness if changed
            if (!string.Equals(room.Name, newName, StringComparison.OrdinalIgnoreCase))
            {
                var sameNameRoom = await _uow.Rooms.GetByNameAsync(newName, ct);
                if (sameNameRoom is not null && sameNameRoom.Id != room.Id)
                    throw new InvalidOperationException($"A room with name '{newName}' already exists.");
                room.Rename(newName);
            }

            if (room.Capacity != request.Capacity)
            {
                room.UpdateCapacity(request.Capacity);
            }

            // NOTE: Domain entity currently has no method to update Location.
            // If needed, add a Room.UpdateLocation(string?) method in the domain layer.
            // For now, ignore Location changes to respect the domain encapsulation.

            _uow.Rooms.Update(room);
            await _uow.SaveChangesAsync(ct);

            return room.ToDto();
        }
    }


    internal sealed class SetRoomActiveHandler : IRequestHandler<SetRoomActiveCommand, RoomDto>

    {
        private readonly IUnitOfWork _uow;

        public SetRoomActiveHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<RoomDto> Handle(SetRoomActiveCommand request, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(request.RoomId, ct);
            if (room is null)
                throw new KeyNotFoundException($"Room '{request.RoomId}' was not found.");

            room.SetActive(request.IsActive);

            _uow.Rooms.Update(room);
            await _uow.SaveChangesAsync(ct);

            return room.ToDto();
        }
    }

    // Rooms - Query Handlers

    internal sealed class GetRoomByIdHandler : IRequestHandler<GetRoomByIdQuery, RoomDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetRoomByIdHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<RoomDto?> Handle(GetRoomByIdQuery request, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(request.RoomId, ct);
            return room?.ToDto();
        }
    }

    internal sealed class GetRoomByNameHandler : IRequestHandler<GetRoomByNameQuery, RoomDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetRoomByNameHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<RoomDto?> Handle(GetRoomByNameQuery request, CancellationToken ct)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Room name is required.", nameof(request.Name));

            var room = await _uow.Rooms.GetByNameAsync(name, ct);
            return room?.ToDto();
        }
    }

    internal sealed class ListActiveRoomsHandler : IRequestHandler<ListActiveRoomsQuery, IReadOnlyList<RoomDto>>
    {
        private readonly IUnitOfWork _uow;

        public ListActiveRoomsHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IReadOnlyList<RoomDto>> Handle(ListActiveRoomsQuery request, CancellationToken ct)
        {
            var rooms = await _uow.Rooms.ListActiveAsync(ct);
            return rooms.Select(r => r.ToDto()).ToList();
        }
    }

    // Bookings - Command Handlers

    internal sealed class CreateBookingHandler : IRequestHandler<CreateBookingCommand, BookingDto>
    {
        private readonly IUnitOfWork _uow;

        public CreateBookingHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken ct)
        {
            if (request.RoomId == Guid.Empty)
                throw new ArgumentException("RoomId is required.", nameof(request.RoomId));
            if (request.CreatedByUserId == Guid.Empty)
                throw new ArgumentException("CreatedByUserId is required.", nameof(request.CreatedByUserId));
            if (request.End <= request.Start)
                throw new ArgumentException("End must be greater than Start.");

            var room = await _uow.Rooms.GetByIdAsync(request.RoomId, ct);
            if (room is null)
                throw new KeyNotFoundException($"Room '{request.RoomId}' was not found.");
            if (!room.IsActive)
                throw new InvalidOperationException("Room is not active for booking.");

            if (request.End <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Booking end must be in the future.");

            // Overlap check
            var hasOverlaps = await _uow.Bookings.HasOverlapsAsync(
                request.RoomId,
                request.Start,
                request.End,
                excludeBookingId: null,
                ct: ct);

            if (hasOverlaps)
                throw new InvalidOperationException("Booking overlaps with an existing booking.");

            var range = TimeRange.Create(request.Start, request.End);
            var booking = Booking.Create(request.RoomId, request.CreatedByUserId, range, request.Subject);

            await _uow.Bookings.AddAsync(booking, ct);
            await _uow.SaveChangesAsync(ct);

            return booking.ToDto();
        }
    }

    internal sealed class ConfirmBookingHandler : IRequestHandler<ConfirmBookingCommand, BookingDto>
    {
        private readonly IUnitOfWork _uow;

        public ConfirmBookingHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> Handle(ConfirmBookingCommand request, CancellationToken ct)
        {
            var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, ct);
            if (booking is null)
                throw new KeyNotFoundException($"Booking '{request.BookingId}' was not found.");

            // Overlap safety check before confirming
            var overlapping = await _uow.Bookings.ListOverlappingAsync(
                booking.RoomId,
                booking.TimeRange.Start,
                booking.TimeRange.End,
                ct);

            // Exclude itself, and exclude cancelled bookings
            overlapping = overlapping
                .Where(b => b.Id != booking.Id && b.Status != BookingStatus.Cancelled)
                .ToList();

            booking.Confirm(overlapping);
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            return booking.ToDto();
        }
    }

    internal sealed class CancelBookingHandler : IRequestHandler<CancelBookingCommand, BookingDto>
    {
        private readonly IUnitOfWork _uow;

        public CancelBookingHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken ct)
        {
            var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, ct);
            if (booking is null)
                throw new KeyNotFoundException($"Booking '{request.BookingId}' was not found.");

            booking.Cancel();
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            return booking.ToDto();
        }
    }

    internal sealed class RescheduleBookingHandler : IRequestHandler<RescheduleBookingCommand, BookingDto>
    {
        private readonly IUnitOfWork _uow;

        public RescheduleBookingHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> Handle(RescheduleBookingCommand request, CancellationToken ct)
        {
            var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, ct);
            if (booking is null)
                throw new KeyNotFoundException($"Booking '{request.BookingId}' was not found.");

            if (request.NewEnd <= request.NewStart)
                throw new ArgumentException("NewEnd must be greater than NewStart.");

            if (request.NewEnd <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("New schedule end must be in the future.");

            // Overlap check (exclude current booking)
            var hasOverlaps = await _uow.Bookings.HasOverlapsAsync(
                booking.RoomId,
                request.NewStart,
                request.NewEnd,
                excludeBookingId: booking.Id,
                ct: ct);

            if (hasOverlaps)
                throw new InvalidOperationException("Reschedule conflicts with another booking.");

            var newRange = TimeRange.Create(request.NewStart, request.NewEnd);

            // For domain-level overlap check, provide list
            var overlapping = await _uow.Bookings.ListOverlappingAsync(
                booking.RoomId,
                request.NewStart,
                request.NewEnd,
                ct);

            overlapping = overlapping
                .Where(b => b.Id != booking.Id && b.Status != BookingStatus.Cancelled)
                .ToList();

            booking.Reschedule(newRange, overlapping);
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            return booking.ToDto();
        }
    }

    internal sealed class CompleteBookingHandler : IRequestHandler<CompleteBookingCommand, BookingDto>
    {
        private readonly IUnitOfWork _uow;

        public CompleteBookingHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CompleteBookingCommand request, CancellationToken ct)
        {
            var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, ct);
            if (booking is null)
                throw new KeyNotFoundException($"Booking '{request.BookingId}' was not found.");

            booking.Complete();
            _uow.Bookings.Update(booking);
            await _uow.SaveChangesAsync(ct);

            return booking.ToDto();
        }
    }

    // Bookings - Query Handlers

    internal sealed class GetBookingByIdHandler : IRequestHandler<GetBookingByIdQuery, BookingDto?>
    {
        private readonly IUnitOfWork _uow;

        public GetBookingByIdHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto?> Handle(GetBookingByIdQuery request, CancellationToken ct)
        {
            var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, ct);
            return booking?.ToDto();
        }
    }

    internal sealed class ListBookingsForRoomHandler : IRequestHandler<ListBookingsForRoomQuery, IReadOnlyList<BookingDto>>
    {
        private readonly IUnitOfWork _uow;

        public ListBookingsForRoomHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IReadOnlyList<BookingDto>> Handle(ListBookingsForRoomQuery request, CancellationToken ct)
        {
            if (request.RoomId == Guid.Empty)
                throw new ArgumentException("RoomId is required.", nameof(request.RoomId));

            if (request.ToExclusive <= request.FromInclusive)
                throw new ArgumentException("ToExclusive must be greater than FromInclusive.");

            var bookings = await _uow.Bookings.ListByRoomAsync(
                request.RoomId,
                request.FromInclusive,
                request.ToExclusive,
                ct);

            return bookings.Select(b => b.ToDto()).ToList();
        }
    }

    internal sealed class CheckRoomAvailabilityHandler : IRequestHandler<CheckRoomAvailabilityQuery, bool>
    {
        private readonly IUnitOfWork _uow;

        public CheckRoomAvailabilityHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<bool> Handle(CheckRoomAvailabilityQuery request, CancellationToken ct)
        {
            if (request.RoomId == Guid.Empty)
                throw new ArgumentException("RoomId is required.", nameof(request.RoomId));

            if (request.End <= request.Start)
                throw new ArgumentException("End must be greater than Start.");

            var room = await _uow.Rooms.GetByIdAsync(request.RoomId, ct);
            if (room is null) return false;
            if (!room.IsActive) return false;

            var hasOverlaps = await _uow.Bookings.HasOverlapsAsync(
                request.RoomId,
                request.Start,
                request.End,
                excludeBookingId: null,
                ct: ct);

            return !hasOverlaps;
        }
    }
}
