using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Interfaces
{
    /// <summary>
    /// Defines a minimal read-only repository abstraction for aggregate roots.
    /// </summary>
    /// <typeparam name="TEntity">The aggregate root type.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    public interface IReadOnlyRepository<TEntity, TId> where TEntity : class
    {
        /// <summary>
        /// Gets an entity by its identifier or null if not found.
        /// </summary>
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);

        /// <summary>
        /// Returns true if an entity with the given identifier exists.
        /// </summary>
        Task<bool> ExistsAsync(TId id, CancellationToken ct = default);
    }

    /// <summary>
    /// Defines a repository abstraction with write operations for aggregate roots.
    /// </summary>
    /// <typeparam name="TEntity">The aggregate root type.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    public interface IRepository<TEntity, TId> : IReadOnlyRepository<TEntity, TId> where TEntity : class
    {
        /// <summary>
        /// Adds a new entity to the persistence context.
        /// </summary>
        Task AddAsync(TEntity entity, CancellationToken ct = default);

        /// <summary>
        /// Marks the entity as modified in the persistence context.
        /// </summary>
        void Update(TEntity entity);

        /// <summary>
        /// Removes the entity from the persistence context.
        /// </summary>
        void Remove(TEntity entity);
    }

    /// <summary>
    /// Repository for managing rooms.
    /// </summary>
    public interface IRoomRepository : IRepository<Room, Guid>
    {
        /// <summary>
        /// Gets a room by its unique name or null if not found.
        /// </summary>
        Task<Room?> GetByNameAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// Lists all active rooms.
        /// </summary>
        Task<IReadOnlyList<Room>> ListActiveAsync(CancellationToken ct = default);

        /// <summary>
        /// Lists all rooms (including inactive).
        /// </summary>
        Task<IReadOnlyList<Room>> ListAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns true if a room with the given name already exists.
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    }

    /// <summary>
    /// Repository for managing bookings.
    /// </summary>
    public interface IBookingRepository : IRepository<Booking, Guid>
    {
        /// <summary>
        /// Lists bookings for a room in the given time range [fromInclusive, toExclusive).
        /// Cancelled bookings may be included depending on implementation needs.
        /// </summary>
        Task<IReadOnlyList<Booking>> ListByRoomAsync(
            Guid roomId,
            DateTimeOffset fromInclusive,
            DateTimeOffset toExclusive,
            CancellationToken ct = default);

        /// <summary>
        /// Returns bookings that overlap with the given time range for a room.
        /// </summary>
        Task<IReadOnlyList<Booking>> ListOverlappingAsync(
            Guid roomId,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct = default);

        /// <summary>
        /// Returns true if any booking overlaps with the given time range for a room.
        /// Optional <paramref name="excludeBookingId"/> can be used when updating an existing booking.
        /// </summary>
        Task<bool> HasOverlapsAsync(
            Guid roomId,
            DateTimeOffset start,
            DateTimeOffset end,
            Guid? excludeBookingId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all bookings (Admin).
        /// </summary>
        Task<IReadOnlyList<Booking>> ListAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Lists bookings created by a specific user.
        /// </summary>
        Task<IReadOnlyList<Booking>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    }

    /// <summary>
    /// Repository for managing users.
    /// </summary>
    public interface IUserRepository : IRepository<User, Guid>
    {
        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Lists all users.
        /// </summary>
        Task<IReadOnlyList<User>> ListAllAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Abstraction for a unit-of-work transaction.
    /// </summary>
    public interface ITransaction : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Commits the transaction.
        /// </summary>
        Task CommitAsync(CancellationToken ct = default);

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        Task RollbackAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Unit of Work abstraction coordinating repositories and transactional boundaries.
    /// </summary>
    public interface IUnitOfWork
    {
        IRoomRepository Rooms { get; }
        IBookingRepository Bookings { get; }
        IUserRepository Users { get; }

        /// <summary>
        /// Persists changes to the underlying store.
        /// Returns the number of state entries written to the database.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Begins a new transaction that should be disposed or committed explicitly.
        /// </summary>
        Task<ITransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
    }
}
