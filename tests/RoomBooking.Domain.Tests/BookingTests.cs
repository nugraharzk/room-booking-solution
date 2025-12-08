using System;
using System.Collections.Generic;
using RoomBooking.Domain.Entities;
// using RoomBooking.Domain.ValueObjects; // TimeRange is in Entities namespace
using Xunit;

namespace RoomBooking.Domain.Tests
{
    public class BookingTests
    {
        [Fact]
        public void Create_WithValidData_ReturnsBooking()
        {
            var roomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var start = DateTimeOffset.UtcNow.AddDays(1);
            var end = start.AddHours(1);
            var range = TimeRange.Create(start, end);

            var booking = Booking.Create(roomId, userId, range, "Meeting");

            Assert.NotNull(booking);
            Assert.Equal(roomId, booking.RoomId);
            Assert.Equal(userId, booking.CreatedByUserId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        [Fact]
        public void Create_WithInvalidRange_ThrowException()
        {
            var start = DateTimeOffset.UtcNow.AddDays(1);
            var end = start.AddHours(-1); // End before Start

            Assert.Throws<ArgumentException>(() => TimeRange.Create(start, end));
        }

        [Fact]
        public void Confirm_WithNoOverlaps_SetsStatusToConfirmed()
        {
            var booking = CreateValidBooking();
            var overlappingBookings = new List<Booking>();

            booking.Confirm(overlappingBookings);

            Assert.Equal(BookingStatus.Confirmed, booking.Status);
        }

        [Fact]
        public void Confirm_WithOverlaps_ThrowsInvalidOperationException()
        {
            var booking = CreateValidBooking();
            // Create overlapping booking for the SAME room
            var otherBooking = Booking.Create(
                booking.RoomId, 
                Guid.NewGuid(), 
                booking.TimeRange, // Same time range = overlap
                "Overlap"
            );

            var overlappingBookings = new List<Booking> { otherBooking };

            Assert.Throws<InvalidOperationException>(() => booking.Confirm(overlappingBookings));
        }

        [Fact]
        public void Cancel_WhenCreated_SetsStatusToCancelled()
        {
            var booking = CreateValidBooking();
            booking.Cancel();
            Assert.Equal(BookingStatus.Cancelled, booking.Status);
        }

        private Booking CreateValidBooking()
        {
            var roomId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var start = DateTimeOffset.Now.AddDays(1);
            var end = start.AddHours(1);
            return Booking.Create(roomId, userId, TimeRange.Create(start, end), "Test");
        }
    }
}
