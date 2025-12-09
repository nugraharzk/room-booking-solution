using System;
using RoomBooking.Domain.Entities;
using Xunit;

namespace RoomBooking.Domain.Tests
{
    public class RoomTests
    {
        [Fact]
        public void Create_WithValidData_ReturnsRoom()
        {
            var name = "Conference Room A";
            var capacity = 10;
            // Room does not have Description property based on Room.cs analysis
            // var description = "A nice room"; 

            var room = Room.Create(name, capacity);

            Assert.NotNull(room);
            Assert.Equal(name, room.Name);
            Assert.Equal(capacity, room.Capacity);
            Assert.True(room.IsActive);
        }

        [Fact]
        public void Create_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Room.Create("", 10));
        }

        [Fact]
        public void Create_WithInvalidCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Room.Create("Room", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Room.Create("Room", -1));
        }

        [Fact]
        public void UpdateCapacity_UpdatesCapacity()
        {
            var room = Room.Create("Old Name", 5);

            room.UpdateCapacity(20);

            Assert.Equal(20, room.Capacity);
        }

        [Fact]
        public void Rename_UpdatesName()
        {
            var room = Room.Create("Old Name", 5);

            room.Rename("New Name");

            Assert.Equal("New Name", room.Name);
        }
    }
}
