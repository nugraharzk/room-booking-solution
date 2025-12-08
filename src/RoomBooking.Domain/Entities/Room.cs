using System;
using System.Collections.Generic;
using System.Linq;

namespace RoomBooking.Domain.Entities
{
    /// <summary>
    /// Represents a physical room that can be booked.
    /// </summary>
    public sealed class Room
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string? Location { get; private set; }
        public int Capacity { get; private set; }
        public bool IsActive { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        // EF Core / Serialization constructor
        private Room() 
        {
            Name = null!;
            Location = null;
        }

        private Room(Guid id, string name, string? location, int capacity, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Room name is required.", nameof(name));
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name.Trim();
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
            Capacity = capacity;
            IsActive = isActive;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public static Room Create(string name, int capacity, string? location = null)
            => new(Guid.NewGuid(), name, location, capacity, isActive: true);

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Room name is required.", nameof(newName));
            Name = newName.Trim();
            Touch();
        }

        public void UpdateCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            Capacity = capacity;
            Touch();
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            Touch();
        }

        /// <summary>
        /// Checks availability against provided bookings. Cancelled bookings are ignored.
        /// Exact boundary contact (requested starts at existing end or vice versa) is allowed.
        /// </summary>
        public bool IsAvailable(TimeRange requested, IEnumerable<Booking> existingBookings)
        {
            if (!IsActive) return false;
            ArgumentNullException.ThrowIfNull(requested);

            var activeBookings = existingBookings?.Where(b => b.Status != BookingStatus.Cancelled) ?? Enumerable.Empty<Booking>();
            return activeBookings.All(b => !b.TimeRange.Overlaps(requested));
        }

        private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Represents a booking for a room, scheduled by an authenticated user.
    /// </summary>
    public sealed class Booking
    {
        private const int MaxSubjectLength = 200;

        public Guid Id { get; private set; }
        public Guid RoomId { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public string? Subject { get; private set; }
        public TimeRange TimeRange { get; private set; }
        public BookingStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? StatusChangedAt { get; private set; }

        // EF Core / Serialization constructor
        private Booking() 
        {
            TimeRange = null!;
        }

        private Booking(Guid id, Guid roomId, Guid createdByUserId, TimeRange timeRange, string? subject)
        {
            if (roomId == Guid.Empty) throw new ArgumentException("RoomId is required.", nameof(roomId));
            if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
            ArgumentNullException.ThrowIfNull(timeRange);

            // Basic time rule: booking must end in the future.
            if (timeRange.End <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Booking end must be in the future.");

            if (!string.IsNullOrWhiteSpace(subject) && subject.Length > MaxSubjectLength)
                throw new ArgumentOutOfRangeException(nameof(subject), $"Subject must be at most {MaxSubjectLength} characters.");

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            RoomId = roomId;
            CreatedByUserId = createdByUserId;
            TimeRange = timeRange;
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
            Status = BookingStatus.Pending;
            CreatedAt = DateTimeOffset.UtcNow;
            StatusChangedAt = null;
        }

        public static Booking Create(Guid roomId, Guid createdByUserId, TimeRange timeRange, string? subject = null)
            => new Booking(Guid.NewGuid(), roomId, createdByUserId, timeRange, subject);

        /// <summary>
        /// Reschedules the booking. Rescheduling returns the booking to Pending.
        /// </summary>
        public void Reschedule(TimeRange newRange, IEnumerable<Booking>? existingBookings = null)
        {
            ArgumentNullException.ThrowIfNull(newRange);
            if (newRange.End <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("New schedule must end in the future.");

            // Prevent overlaps with other active bookings for the same room.
            if (existingBookings is not null && existingBookings
                    .Where(b => b.Id != Id && b.RoomId == RoomId && b.Status != BookingStatus.Cancelled)
                    .Any(b => b.TimeRange.Overlaps(newRange)))
            {
                throw new InvalidOperationException("Cannot reschedule due to overlap with another booking.");
            }

            TimeRange = newRange;
            SetStatus(BookingStatus.Pending);
        }

        /// <summary>
        /// Confirms the booking, ensuring no overlap with provided bookings.
        /// </summary>
        public void Confirm(IEnumerable<Booking>? existingBookings = null)
        {
            if (Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("Cannot confirm a cancelled booking.");

            if (existingBookings is not null && existingBookings
                    .Where(b => b.Id != Id && b.RoomId == RoomId && b.Status != BookingStatus.Cancelled)
                    .Any(b => b.TimeRange.Overlaps(TimeRange)))
            {
                throw new InvalidOperationException("Cannot confirm due to overlap with another booking.");
            }

            SetStatus(BookingStatus.Confirmed);
        }

        /// <summary>
        /// Cancels the booking. Can be called from any status; idempotent.
        /// </summary>
        public void Cancel()
        {
            if (Status == BookingStatus.Cancelled) return;
            SetStatus(BookingStatus.Cancelled);
        }

        /// <summary>
        /// Completes a booking. Only allowed for confirmed bookings after the time range has ended.
        /// </summary>
        public void Complete()
        {
            if (Status != BookingStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed bookings can be completed.");

            if (TimeRange.End > DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Cannot complete booking before it ends.");

            SetStatus(BookingStatus.Completed);
        }

        public void UpdateSubject(string? subject)
        {
            if (!string.IsNullOrWhiteSpace(subject) && subject.Length > MaxSubjectLength)
                throw new ArgumentOutOfRangeException(nameof(subject), $"Subject must be at most {MaxSubjectLength} characters.");
            Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        }

        private void SetStatus(BookingStatus status)
        {
            Status = status;
            StatusChangedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Lifecycle status of a booking.
    /// </summary>
    public enum BookingStatus
    {
        Pending = 0,
        Confirmed = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Immutable value object representing a time interval.
    /// End must be strictly greater than Start.
    /// </summary>
    public sealed class TimeRange : IEquatable<TimeRange>
    {
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }

        private TimeRange(DateTimeOffset start, DateTimeOffset end)
        {
            if (end <= start)
                throw new ArgumentException("End must be greater than Start.");
            Start = start;
            End = end;
        }

        public static TimeRange Create(DateTimeOffset start, DateTimeOffset end)
            => new(start, end);

        public TimeSpan Duration => End - Start;

        /// <summary>
        /// Returns true if ranges overlap (exclusive of touching boundaries).
        /// Example: [1,2] overlaps [1.5,3] but does not overlap [2,3].
        /// </summary>
        public bool Overlaps(TimeRange other)
        {
            if (other is null) return false;
            return Start < other.End && other.Start < End;
        }

        /// <summary>
        /// Returns true if the instant is within [Start, End] inclusive.
        /// </summary>
        public bool Contains(DateTimeOffset instant)
            => instant >= Start && instant <= End;

        public bool Equals(TimeRange? other)
            => other is not null && Start.Equals(other.Start) && End.Equals(other.End);

        public override bool Equals(object? obj) => Equals(obj as TimeRange);

        public override int GetHashCode() => HashCode.Combine(Start, End);

        public TimeRange Shift(TimeSpan delta)
            => Create(Start + delta, End + delta);

        public TimeRange Expand(TimeSpan delta)
            => Create(Start, End + delta);

        public override string ToString() => $"{Start:u} - {End:u}";
    }
}
