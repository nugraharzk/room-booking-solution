using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Bookings;
using RoomBooking.Infrastructure.Auth;

namespace RoomBooking.API.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    public sealed class RoomsController : ControllerBase
    {
        private readonly ISender _mediator;

        public RoomsController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new room.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomCommand command, CancellationToken ct)
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Gets a room by its identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetRoomByIdQuery(id), ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Gets a room by its unique name.
        /// </summary>
        [HttpGet("by-name/{name}")]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomDto>> GetByName([FromRoute] string name, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetRoomByNameQuery(name), ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>
        /// Lists active rooms.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Policies.BookingsRead)]
        [ProducesResponseType(typeof(IReadOnlyList<RoomDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoomDto>>> ListActive(CancellationToken ct)
        {
            var list = await _mediator.Send(new ListActiveRoomsQuery(), ct);
            return Ok(list);
        }

        /// <summary>
        /// Updates room details (name, capacity, optional location).
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.BookingsWrite)]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoomDto>> Update([FromRoute] Guid id, [FromBody] UpdateRoomRequest body, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new UpdateRoomDetailsCommand(id, body.Name, body.Capacity, body.Location),
                ct);

            return Ok(result);
        }

        /// <summary>
        /// Sets room active/inactive.
        /// </summary>
        [HttpPatch("{id:guid}/active")]
        [Authorize(Policy = Policies.RequireManager)]
        [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomDto>> SetActive([FromRoute] Guid id, [FromBody] SetRoomActiveRequest body, CancellationToken ct)
        {
            var result = await _mediator.Send(new SetRoomActiveCommand(id, body.IsActive), ct);
            return Ok(result);
        }
    }

    /// <summary>
    /// API request contract for updating a room.
    /// </summary>
    public sealed record UpdateRoomRequest(
        string Name,
        int Capacity,
        string? Location
    );

    /// <summary>
    /// API request contract for toggling room active state.
    /// </summary>
    public sealed record SetRoomActiveRequest(bool IsActive);
}
