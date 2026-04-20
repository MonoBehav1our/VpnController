using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VpnController.Repositories;
using VpnController.Services;

namespace VpnController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class XrayController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly XrayConfigGenerator _xrayConfigGenerator;
    private readonly ClientSubscriptionBuilder _clientSubscriptionBuilder;
    private readonly SotaSubscriptionRefreshService _sotaSubscriptionRefreshService;

    public XrayController(UserRepository userRepository,
        XrayConfigGenerator xrayConfigGenerator,
        ClientSubscriptionBuilder clientSubscriptionBuilder,
        SotaSubscriptionRefreshService sotaSubscriptionRefreshService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _xrayConfigGenerator = xrayConfigGenerator ?? throw new ArgumentNullException(nameof(xrayConfigGenerator));
        _clientSubscriptionBuilder = clientSubscriptionBuilder ?? throw new ArgumentNullException(nameof(clientSubscriptionBuilder));
        _sotaSubscriptionRefreshService = sotaSubscriptionRefreshService ?? throw new ArgumentNullException(nameof(sotaSubscriptionRefreshService));
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        try
        {
            var root = await _xrayConfigGenerator.Build();
            var json = XrayConfigGenerator.ToIndentedJson(root);
            return Content(json, "application/json");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("subscription/{userId:guid}")]
    public async Task<IActionResult> GetUserSubscription(Guid userId, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false) is not { } user)
        {
            return NotFound();
        }

        try
        {
            var lines = _clientSubscriptionBuilder.BuildLinesForUser(user);
            var text = string.Join("\n", lines);
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            return Content(body, "text/plain", Encoding.UTF8);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshConfig(CancellationToken cancellationToken)
    {
        try
        {
            await _sotaSubscriptionRefreshService.RefreshAsync(cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }
}