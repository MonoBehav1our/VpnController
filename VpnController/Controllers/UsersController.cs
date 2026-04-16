using Microsoft.AspNetCore.Mvc;
using VpnController.Data;
using VpnController.Repositories;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserRepository _users;

    public UsersController(UserRepository users)
    {
        _users = users;
    }

    [HttpPost]
    [Produces("application/json")]
    public async Task<ActionResult<User>> Create(CancellationToken cancellationToken)
    {
        var user = await _users.CreateAsync(cancellationToken);
        return Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _users.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
