using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Users;
using RoomBooking.Infrastructure.Auth;

namespace RoomBooking.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public sealed class UsersController : ControllerBase
    {
        private readonly ISender _mediator;

        public UsersController(ISender mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Lists all users.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(UserDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserDto[]>> ListAll(CancellationToken ct)
        {
            var users = await _mediator.Send(new ListUsersQuery(), ct);
            return Ok(users);
        }

        /// <summary>
        /// Updates a user's role.
        /// </summary>
        [HttpPatch("{id:guid}/role")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UpdateRole([FromRoute] Guid id, [FromBody] UpdateUserRoleRequest body, CancellationToken ct)
        {
            var result = await _mediator.Send(new UpdateUserRoleCommand(id, body.Role), ct);
            return Ok(result);
        }

        /// <summary>
        /// Toggles a user's active status.
        /// </summary>
        [HttpPatch("{id:guid}/active")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> ToggleActive([FromRoute] Guid id, [FromBody] ToggleUserActiveRequest body, CancellationToken ct)
        {
            var result = await _mediator.Send(new ToggleUserActiveCommand(id, body.IsActive), ct);
            return Ok(result);
        }
    }

    public record UpdateUserRoleRequest(string Role);
    public record ToggleUserActiveRequest(bool IsActive);
}
