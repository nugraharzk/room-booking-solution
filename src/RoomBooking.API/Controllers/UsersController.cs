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

        /// <summary>
        /// Creates a new user.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest body, CancellationToken ct)
        {
            var command = new CreateUserCommand(body.Email, body.Password, body.FirstName, body.LastName, body.Role);
            var result = await _mediator.Send(command, ct);
            // No GetById endpoint exposed yet in this controller, so we just return Created
            return StatusCode(StatusCodes.Status201Created, result);
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteUserCommand(id), ct);
            return NoContent();
        }
    }

    public record UpdateUserRoleRequest(string Role);
    public record ToggleUserActiveRequest(bool IsActive);
    public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);
}
