using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VpnController.Helpers;
using VpnController.Options;
using VpnController.Repositories;
using VpnController.Services;

namespace VpnController.Controllers;

[ApiController]
[Route("api/singin")]
public class VpnRuntimeController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly XrayConfigGenerator _xrayConfigGenerator;
    private readonly ClientSubscriptionBuilder _clientSubscriptionBuilder;
    private readonly SotaSubscriptionRefreshService _sotaSubscriptionRefreshService;
    private readonly ApiAccessOptions _apiAccess;

    public VpnRuntimeController(
        UserRepository userRepository,
        XrayConfigGenerator xrayConfigGenerator,
        ClientSubscriptionBuilder clientSubscriptionBuilder,
        SotaSubscriptionRefreshService sotaSubscriptionRefreshService,
        IOptions<ApiAccessOptions> apiAccessOptions)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _xrayConfigGenerator = xrayConfigGenerator ?? throw new ArgumentNullException(nameof(xrayConfigGenerator));
        _clientSubscriptionBuilder = clientSubscriptionBuilder ?? throw new ArgumentNullException(nameof(clientSubscriptionBuilder));
        _sotaSubscriptionRefreshService = sotaSubscriptionRefreshService ?? throw new ArgumentNullException(nameof(sotaSubscriptionRefreshService));
        _apiAccess = apiAccessOptions?.Value ?? throw new ArgumentNullException(nameof(apiAccessOptions));
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        if (!BearerTokenAuthHelper.IsValid(Request, _apiAccess.BearerToken))
            return Unauthorized();

        try
        {
            var root = await _xrayConfigGenerator.Build();
            return Content(root, "application/json");
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
        if (!BearerTokenAuthHelper.IsValid(Request, _apiAccess.BearerToken))
            return Unauthorized();

        try
        {
            await _sotaSubscriptionRefreshService.RefreshAsync(cancellationToken);
            return Ok();
        }
        catch (Exception)
        {
            return Problem();
        }
    }
}
