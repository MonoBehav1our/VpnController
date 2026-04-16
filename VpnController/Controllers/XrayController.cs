using System.Text;
using Microsoft.AspNetCore.Mvc;
using VpnController.Repositories;
using VpnController.Services;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class XrayController : ControllerBase
{
    private readonly UserRepository _users;
    private readonly SubscriptionRepository _subscriptions;
    private readonly XrayConfigGenerator _generator;
    private readonly VlessClientSubscriptionBuilder _clientSubscription;
    private readonly XrayRestartService _xrayRestart;

    public XrayController(
        UserRepository users,
        SubscriptionRepository subscriptions,
        XrayConfigGenerator generator,
        VlessClientSubscriptionBuilder clientSubscription,
        XrayRestartService xrayRestart)
    {
        _users = users;
        _subscriptions = subscriptions;
        _generator = generator;
        _clientSubscription = clientSubscription;
        _xrayRestart = xrayRestart;
    }

    /// <summary>
    /// Записать конфиг на диск (как GET config) и выполнить Xray:Restart:RestartCommand.
    /// </summary>
    [HttpPost("restart")]
    public async Task<IActionResult> Restart(CancellationToken cancellationToken)
    {
        var r = await _xrayRestart.WriteConfigAndRestartAsync(cancellationToken);
        if (!r.Ok)
        {
            return StatusCode(r.StatusCode, new { detail = r.Detail });
        }

        return NoContent();
    }

    /// <summary>
    /// Готовый JSON-конфиг Xray: 10 инбаундов (direct-in и 9 по странам из подписки), в каждом — все пользователи;
    /// аутбаунды direct + sota-01..09 из строк подписки.
    /// </summary>
    [HttpGet("config")]
    [Produces("application/json")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        if (!_subscriptions.TryGetLines(out var lines))
        {
            return NotFound();
        }

        if (!SubscriptionSotaOutboundsResolver.TryResolve(lines, out var sotaOutbounds))
        {
            return BadRequest();
        }

        var users = await _users.GetAllAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();

        try
        {
            var root = _generator.Build(userIds, sotaOutbounds);
            var json = XrayConfigGenerator.ToIndentedJson(root);
            return Content(json, "application/json");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Подписка для клиента (HAPP и др.): base64, как у SOTA — внутри UTF-8 со всеми vless-строками по инбаундам;
    /// адрес и Reality (shortId, pbk, sni) из <c>Xray:*</c>, UUID — пользователь из маршрута.
    /// </summary>
    [HttpGet("subscription/{userId:guid}")]
    [Produces("text/plain")]
    public async Task<IActionResult> GetUserSubscription(Guid userId, CancellationToken cancellationToken)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            var lines = _clientSubscription.BuildLinesForUser(userId);
            
            //to base64 format for clients
            var text = string.Join("\n", lines);
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            
            return Content(body, "text/plain", Encoding.UTF8);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
