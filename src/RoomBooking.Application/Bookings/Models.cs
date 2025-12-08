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
        [property: Required, MinLength(1)] string Name,
        [property: Range(1, int.MaxValue)] int Capacity,
        string? Location
    ) : IRequest<RoomDto>;

    public sealed record UpdateRoomDetailsCommand(
        [property: Required] Guid RoomId,
        [property: Required, MinLength(1)] string Name,
        [property: Range(1, int.MaxValue)] int Capacity,
        string? Location
    ) : IRequest<RoomDto>;

    public sealed record SetRoomActiveCommand(
        [property: Required] Guid RoomId,
        bool IsActive
    ) : IRequest<RoomDto>;

    // Rooms - Queries

    public sealed record GetRoomByIdQuery(
        [property: Required] Guid RoomId
    ) : IRequest<RoomDto?>;

    public sealed record GetRoomByNameQuery(
        [property: Required, MinLength(1)] string Name
    ) : IRequest<RoomDto?>;

    public sealed record ListActiveRoomsQuery() : IRequest<IReadOnlyList<RoomDto>>;

    public sealed record ListAllRoomsQuery() : IRequest<IReadOnlyList<RoomDto>>;

    // Bookings - Commands

    public sealed record CreateBookingCommand(
        [property: Required] Guid RoomId,
        [property: Required] Guid CreatedByUserId,
        DateTimeOffset Start,
        DateTimeOffset End,
        [property: MaxLength(200)] string? Subject
    ) : IRequest<BookingDto>;

    public sealed record ConfirmBookingCommand(
        [property: Required] Guid BookingId
    ) : IRequest<BookingDto>;

    public sealed record CancelBookingCommand(
        [property: Required] Guid BookingId
    ) : IRequest<BookingDto>;

    public sealed record RescheduleBookingCommand(
        [property: Required] Guid BookingId,
        DateTimeOffset NewStart,
        DateTimeOffset NewEnd
    ) : IRequest<BookingDto>;

    public sealed record CompleteBookingCommand(
        [property: Required] Guid BookingId
    ) : IRequest<BookingDto>;

    // Bookings - Queries

    public sealed record GetBookingByIdQuery(
        [property: Required] Guid BookingId
    ) : IRequest<BookingDto?>;

    public sealed record ListBookingsForRoomQuery(
        [property: Required] Guid RoomId,
        DateTimeOffset FromInclusive,
        DateTimeOffset ToExclusive
    ) : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record ListAllBookingsQuery() : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record ListMyBookingsQuery(
        [property: Required] Guid UserId
    ) : IRequest<IReadOnlyList<BookingDto>>;

    public sealed record CheckRoomAvailabilityQuery(
        [property: Required] Guid RoomId,
        DateTimeOffset Start,
        DateTimeOffset End
    ) : IRequest<bool>;
}
