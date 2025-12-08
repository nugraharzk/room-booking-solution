using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Bookings;
using RoomBooking.Infrastructure.Auth;

namespace RoomBooking.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public sealed class BookingsController : ControllerBase
    {
        private readonly ISender _mediator;

        public BookingsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new booking for a room.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest body, CancellationToken ct)
        {
            if (body is null) return BadRequest("Request body is required.");

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized("User identifier claim is missing or invalid.");
            }

            var cmd = new CreateBookingCommand(
                RoomId: body.RoomId,
                CreatedByUserId: userId,
                Start: body.Start,
                End: body.End,
                Subject: body.Subject
            );

            var result = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Gets a booking by its identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetBookingByIdQuery(id), ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Lists all bookings (Admin only).
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = Policies.RequireAdmin)]
        [ProducesResponseType(typeof(BookingDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<BookingDto[]>> ListAll(CancellationToken ct)
        {
            var list = await _mediator.Send(new ListAllBookingsQuery(), ct);
            return Ok(list);
        }

        /// <summary>
        /// Lists bookings for the current user.
        /// </summary>
        [HttpGet("my")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(BookingDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<BookingDto[]>> ListMy(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var list = await _mediator.Send(new ListMyBookingsQuery(userId), ct);
            return Ok(list);
        }

        /// <summary>
        /// Lists bookings for a room within a time window.
        /// </summary>
        [HttpGet("room/{roomId:guid}")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(BookingDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BookingDto[]>> ListForRoom(
            [FromRoute] Guid roomId,
            [FromQuery] DateTimeOffset from,
            [FromQuery] DateTimeOffset to,
            CancellationToken ct)
        {
            if (roomId == Guid.Empty) return BadRequest("roomId is required.");
            if (to <= from) return BadRequest("Query parameter 'to' must be greater than 'from'.");

            var list = await _mediator.Send(new ListBookingsForRoomQuery(roomId, from, to), ct);
            return Ok(list);
        }

        /// <summary>
        /// Confirms a booking.
        /// </summary>
        [HttpPost("{id:guid}/confirm")]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDto>> Confirm([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new ConfirmBookingCommand(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Cancels a booking.
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDto>> Cancel([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CancelBookingCommand(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Completes a booking (only allowed after it ends and if confirmed).
        /// </summary>
        [HttpPost("{id:guid}/complete")]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDto>> Complete([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CompleteBookingCommand(id), ct);
            return Ok(result);
        }

        /// <summary>
        /// Reschedules a booking to a new time window.
        /// </summary>
        [HttpPatch("{id:guid}/reschedule")]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDto>> Reschedule([FromRoute] Guid id, [FromBody] RescheduleBookingRequest body, CancellationToken ct)
        {
            if (body is null) return BadRequest("Request body is required.");

            var result = await _mediator.Send(
                new RescheduleBookingCommand(id, body.NewStart, body.NewEnd),
                ct);
            return Ok(result);
        }

        /// <summary>
        /// Checks if a room is available for the given time range.
        /// </summary>
        [HttpGet("availability")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> CheckAvailability(
            [FromQuery] Guid roomId,
            [FromQuery] DateTimeOffset start,
            [FromQuery] DateTimeOffset end,
            CancellationToken ct)
        {
            if (roomId == Guid.Empty) return BadRequest("roomId is required.");
            if (end <= start) return BadRequest("Query parameter 'end' must be greater than 'start'.");

            var available = await _mediator.Send(new CheckRoomAvailabilityQuery(roomId, start, end), ct);
            return Ok(available);
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = default;
            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("sub");

            if (claim is null) return false;
            return Guid.TryParse(claim.Value, out userId);
        }
    }

    /// <summary>
        /// API request contract for creating a booking.
        /// </summary>
    public sealed record CreateBookingRequest(
        Guid RoomId,
        DateTimeOffset Start,
        DateTimeOffset End,
        string? Subject
    );

    /// <summary>
    /// API request contract for rescheduling a booking.
    /// </summary>
    public sealed record RescheduleBookingRequest(
        DateTimeOffset NewStart,
        DateTimeOffset NewEnd
    );
}
