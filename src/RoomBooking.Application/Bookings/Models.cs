using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MediatR;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Bookings
{
    // DTOs

    public sealed record RoomDto(
        Guid Id,
        string Name,
        string? Location,
        int Capacity,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    );

    public sealed record BookingDto(
        Guid Id,
        Guid RoomId,
        Guid CreatedByUserId,
        string? Subject,
        DateTimeOffset Start,
        DateTimeOffset End,
        BookingStatus Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StatusChangedAt
    );

    // Mapping helpers

    public static class MappingExtensions
    {
        public static RoomDto ToDto(this Room entity) =>
            new(
                entity.Id,
                entity.Name,
                entity.Location,
                entity.Capacity,
                entity.IsActive,
                entity.CreatedAt,
                entity.UpdatedAt
            );

        public static BookingDto ToDto(this Booking entity) =>
            new(
                entity.Id,
                entity.RoomId,
                entity.CreatedByUserId,
                entity.Subject,
                entity.TimeRange.Start,
                entity.TimeRange.End,
                entity.Status,
                entity.CreatedAt,
                entity.StatusChangedAt
            );
    }

    // Rooms - Commands

    public sealed record CreateRoomCommand(
        [Required, MinLength(1)] string Name,
        [Range(1, int.MaxValue)] int Capacity,
        string? Location
    ) : IRequest<RoomDto>;

    public sealed record UpdateRoomDetailsCommand(
        [Required] Guid RoomId,
        [Required, MinLength(1)] string Name,
        [Range(1, int.MaxValue)] int Capacity,
        string? Location
    ) : IRequest<RoomDto>;

    public sealed record SetRoomActiveCommand(
        [Required] Guid RoomId,
        bool IsActive
    ) : IRequest<RoomDto>;

    public sealed record DeleteRoomCommand(
        [Required] Guid RoomId
    ) : IRequest<Unit>;

    // Rooms - Queries

    public sealed record GetRoomByIdQuery(
        [Required] Guid RoomId
    ) : IRequest<RoomDto?>;

    public sealed record GetRoomByNameQuery(
        [Required, MinLength(1)] string Name
    ) : IRequest<RoomDto?>;

    public sealed record ListActiveRoomsQuery() : IRequest<IReadOnlyList<RoomDto>>;

    public sealed record ListAllRoomsQuery() : IRequest<IReadOnlyList<RoomDto>>;

    // Bookings - Commands

    public sealed record CreateBookingCommand(
        [Required] Guid RoomId,
        [Required] Guid CreatedByUserId,
        DateOnly Date,
        [MaxLength(200)] string? Subject
    ) : IRequest<BookingDto>;

    public sealed record ConfirmBookingCommand(
        [Required] Guid BookingId
    ) : IRequest<BookingDto>;

    public sealed record CancelBookingCommand(
        [Required] Guid BookingId
    ) : IRequest<BookingDto>;

    public sealed record DeleteBookingCommand(
        [Required] Guid BookingId
    ) : IRequest<Unit>;

    public sealed record RescheduleBookingCommand(
        [Required] Guid BookingId,
        DateTimeOffset NewStart,
        DateTimeOffset NewEnd
    ) : IRequest<BookingDto>;

    public sealed record CompleteBookingCommand(
        [Required] Guid BookingId
    ) : IRequest<BookingDto>;

    // Bookings - Queries

    public sealed record GetBookingByIdQuery(
        [Required] Guid BookingId
    ) : IRequest<BookingDto?>;

    public sealed record ListBookingsForRoomQuery(
        [Required] Guid RoomId,
        DateTimeOffset FromInclusive,
        DateTimeOffset ToExclusive
    ) : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record ListAllBookingsQuery() : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record ListMyBookingsQuery(
        [Required] Guid UserId
    ) : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record CheckRoomAvailabilityQuery(
        [Required] Guid RoomId,
        DateTimeOffset Start,
        DateTimeOffset End
    ) : IRequest<bool>;
}
