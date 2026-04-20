using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VpnController.Data;
using VpnController.Helpers;
using VpnController.Options;
using VpnController.Repositories;

namespace VpnController.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly ApiAccessOptions _apiAccess;

    public UsersController(UserRepository userRepository, IOptions<ApiAccessOptions> apiAccessOptions)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _apiAccess = apiAccessOptions?.Value ?? throw new ArgumentNullException(nameof(apiAccessOptions));
    }

    [HttpPost("{alias}")]
    public async Task<ActionResult<User>> Create([FromRoute] string alias, CancellationToken cancellationToken)
    {
        if (!BearerTokenAuthHelper.IsValid(Request, _apiAccess.BearerToken))
            return Unauthorized();

        try
        {
            var user = await _userRepository.CreateAsync(alias, cancellationToken).ConfigureAwait(false);
            return StatusCode(201, user);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { detail = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!BearerTokenAuthHelper.IsValid(Request, _apiAccess.BearerToken))
            return Unauthorized();

        var deleted = await _userRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
