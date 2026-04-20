using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VpnController.Data;
using VpnController.Repositories;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public UsersController(UserRepository userRepository)
    {
        _userRepository = userRepository ??  throw new ArgumentNullException(nameof(userRepository));
    }
    
    [HttpPost("{alias}")]
    public async Task<ActionResult<User>> Create([FromRoute] string alias, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.CreateAsync(alias, cancellationToken).ConfigureAwait(false);
            return StatusCode(201, user);;
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
        var deleted = await _userRepository.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
