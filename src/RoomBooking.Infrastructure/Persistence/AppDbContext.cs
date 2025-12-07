#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoomBooking.Application.Interfaces;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core DbContext for Room Booking system.
    /// </summary>
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Booking> Bookings => Set<Booking>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureRoom(modelBuilder);
            ConfigureBooking(modelBuilder);
        }

        private static void ConfigureRoom(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Room>();

            entity.ToTable("Rooms");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(r => r.Name)
                .IsUnique();

            entity.Property(r => r.Location)
                .HasMaxLength(200);

            entity.Property(r => r.Capacity)
                .IsRequired();

            entity.Property(r => r.IsActive)
                .IsRequired();

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.Property(r => r.UpdatedAt);

            entity.HasIndex(r => r.IsActive);
        }

        private static void ConfigureBooking(ModelBuilder modelBuilder)
        {
            const int SubjectMaxLength = 200;

            var entity = modelBuilder.Entity<Booking>();

            entity.ToTable("Bookings");

            entity.HasKey(b => b.Id);

            entity.Property(b => b.RoomId)
                .IsRequired();

            entity.Property(b => b.CreatedByUserId)
                .IsRequired();

            entity.Property(b => b.Subject)
                .HasMaxLength(SubjectMaxLength);

            entity.Property(b => b.Status)
                .IsRequired();

            entity.Property(b => b.CreatedAt)
                .IsRequired();

            entity.Property(b => b.StatusChangedAt);

            // Relationship (no navigation properties on domain entity)
            entity.HasOne<Room>()
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Value object mapping for TimeRange
            entity.OwnsOne(b => b.TimeRange, tr =>
            {
                tr.Property(p => p.Start)
                  .HasColumnName("StartAt")
                  .IsRequired();

                tr.Property(p => p.End)
                  .HasColumnName("EndAt")
                  .IsRequired();

                tr.WithOwner();
            });


            // Helpful indexes for range queries

            entity.HasIndex(b => new { b.RoomId, b.Status });

        }
    }

    // ---------------------------
    // Repository Implementations
    // ---------------------------

    internal sealed class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _db;

        public RoomRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Room entity, CancellationToken ct = default)
        {
            await _db.Rooms.AddAsync(entity, ct).ConfigureAwait(false);
        }

        public void Remove(Room entity)
        {
            _db.Rooms.Remove(entity);
        }

        public void Update(Room entity)
        {
            _db.Rooms.Update(entity);
        }

        public async Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Rooms
                .FirstOrDefaultAsync(r => r.Id == id, ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Rooms
                .AsNoTracking()
                .AnyAsync(r => r.Id == id, ct)
                .ConfigureAwait(false);
        }

        public async Task<Room?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            // Rely on SQL Server's default case-insensitive collation for equality.
            var trimmed = name.Trim();
            return await _db.Rooms
                .FirstOrDefaultAsync(r => r.Name == trimmed, ct)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Room>> ListActiveAsync(CancellationToken ct = default)
        {
            return await _db.Rooms
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        {
            var trimmed = name.Trim();
            return await _db.Rooms
                .AsNoTracking()
                .AnyAsync(r => r.Name == trimmed, ct)
                .ConfigureAwait(false);
        }
    }

    internal sealed class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _db;

        public BookingRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Booking entity, CancellationToken ct = default)
        {
            await _db.Bookings.AddAsync(entity, ct).ConfigureAwait(false);
        }

        public void Remove(Booking entity)
        {
            _db.Bookings.Remove(entity);
        }

        public void Update(Booking entity)
        {
            _db.Bookings.Update(entity);
        }

        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Bookings
                .FirstOrDefaultAsync(b => b.Id == id, ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Bookings
                .AsNoTracking()
                .AnyAsync(b => b.Id == id, ct)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Booking>> ListByRoomAsync(
            Guid roomId,
            DateTimeOffset fromInclusive,
            DateTimeOffset toExclusive,
            CancellationToken ct = default)
        {
            // Return bookings that overlap the requested window.
            // Overlap condition: start < toExclusive && fromInclusive < end
            return await _db.Bookings
                .AsNoTracking()
                .Where(b =>
                    b.RoomId == roomId &&
                    b.TimeRange.Start < toExclusive &&
                    fromInclusive < b.TimeRange.End)
                .OrderBy(b => b.TimeRange.Start)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<Booking>> ListOverlappingAsync(
            Guid roomId,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct = default)
        {
            return await _db.Bookings
                .AsNoTracking()
                .Where(b =>
                    b.RoomId == roomId &&
                    b.Status != BookingStatus.Cancelled &&
                    b.TimeRange.Start < end &&
                    start < b.TimeRange.End)
                .OrderBy(b => b.TimeRange.Start)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        public async Task<bool> HasOverlapsAsync(
            Guid roomId,
            DateTimeOffset start,
            DateTimeOffset end,
            Guid? excludeBookingId = null,
            CancellationToken ct = default)
        {
            var query = _db.Bookings.AsNoTracking().Where(b =>
                b.RoomId == roomId &&
                b.Status != BookingStatus.Cancelled &&
                b.TimeRange.Start < end &&
                start < b.TimeRange.End);

            if (excludeBookingId.HasValue)
            {
                var excluded = excludeBookingId.Value;
                query = query.Where(b => b.Id != excluded);
            }

            return await query.AnyAsync(ct).ConfigureAwait(false);
        }
    }

    // ---------------------------
    // Unit of Work implementation
    // ---------------------------

    public sealed class EfUnitOfWork : IUnitOfWork, IAsyncDisposable, IDisposable
    {
        private readonly AppDbContext _dbContext;

        public EfUnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            Rooms = new RoomRepository(dbContext);
            Bookings = new BookingRepository(dbContext);
        }

        public IRoomRepository Rooms { get; }
        public IBookingRepository Bookings { get; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _dbContext.SaveChangesAsync(ct);

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            var tx = await _dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            return new EfTransaction(tx);
        }

        public ValueTask DisposeAsync()
        {
            return _dbContext.DisposeAsync();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    internal sealed class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _inner;

        public EfTransaction(IDbContextTransaction inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            await _inner.CommitAsync(ct).ConfigureAwait(false);
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            await _inner.RollbackAsync(ct).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            _inner.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
