using Microsoft.AspNetCore.Mvc;
using VpnController.Services;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionStringsController : ControllerBase
{
    private readonly InMemorySubscriptionStore _store;

    public ConnectionStringsController(InMemorySubscriptionStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Строки единственной подписки из in-memory кэша (обновляется фоновой задачей по <c>Subscriptions:SubscriptionGuid</c>).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    public ActionResult<IReadOnlyList<string>> Get()
    {
        if (!_store.TryGetLines(out var lines))
        {
            return NotFound();
        }

        return Ok(lines);
    }
}
