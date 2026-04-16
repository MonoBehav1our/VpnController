using Microsoft.AspNetCore.Mvc;
using VpnController.Services;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionStringsController : ControllerBase
{
    private readonly SubscriptionRepository _repository;

    public ConnectionStringsController(SubscriptionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Строки единственной подписки из in-memory кэша (обновляется фоновой задачей по <c>Subscriptions:SubscriptionGuid</c>).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    public ActionResult<IReadOnlyList<string>> Get()
    {
        if (!_repository.TryGetLines(out var lines))
        {
            return NotFound();
        }

        return Ok(lines);
    }
}
