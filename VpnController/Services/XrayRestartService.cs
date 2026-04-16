using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using VpnController.Repositories;

namespace VpnController.Services;

/// <summary>
/// Запись актуального JSON конфига и выполнение команды перезапуска xray.
/// </summary>
public sealed class XrayRestartService
{
    private readonly IOptions<XrayRestartOptions> _options;
    private readonly SubscriptionRepository _repository;
    private readonly XrayConfigGenerator _generator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<XrayRestartService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public XrayRestartService(
        IOptions<XrayRestartOptions> options,
        SubscriptionRepository repository,
        XrayConfigGenerator generator,
        IServiceScopeFactory scopeFactory,
        ILogger<XrayRestartService> logger)
    {
        _options = options;
        _repository = repository;
        _generator = generator;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<XrayRestartResult> WriteConfigAndRestartAsync(CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
        {
            return new XrayRestartResult(false, StatusCodes.Status503ServiceUnavailable,
                "Xray:Restart:Enabled = false.");
        }

        if (string.IsNullOrWhiteSpace(opts.ConfigFilePath))
        {
            _logger.LogWarning("Xray:Restart:ConfigFilePath is empty");
            return new XrayRestartResult(false, StatusCodes.Status503ServiceUnavailable,
                "Xray:Restart:ConfigFilePath is not set.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_repository.TryGetLines(out var lines))
            {
                _logger.LogDebug("No subscription lines in store");
                return new XrayRestartResult(false, StatusCodes.Status400BadRequest,
                    "Нет данных подписки в памяти (сначала дождитесь фетча или проверьте Subscriptions).");
            }

            if (!SubscriptionSotaOutboundsResolver.TryResolve(lines, out var sotaOutbounds))
            {
                _logger.LogWarning("Subscription lines invalid for xray config");
                return new XrayRestartResult(false, StatusCodes.Status400BadRequest, "Invalid xray config");
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserRepository>();
            var userList = await users.GetAllAsync(cancellationToken);
            var userIds = userList.Select(u => u.Id).ToList();

            JsonObject root;
            try
            {
                root = _generator.Build(userIds, sotaOutbounds);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Xray config build failed");
                return new XrayRestartResult(false, StatusCodes.Status500InternalServerError, ex.Message);
            }

            var json = XrayConfigGenerator.ToIndentedJson(root);
            await WriteAtomicAsync(opts.ConfigFilePath, json, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Wrote xray config to {Path}", opts.ConfigFilePath);

            if (string.IsNullOrWhiteSpace(opts.RestartCommand))
            {
                return new XrayRestartResult(true, StatusCodes.Status204NoContent, null);
            }

            var restartOk = await RunRestartCommandAsync(opts.RestartCommand, cancellationToken).ConfigureAwait(false);
            if (!restartOk)
            {
                return new XrayRestartResult(false, StatusCodes.Status502BadGateway,
                    "Конфиг записан, но команда перезапуска завершилась с ошибкой (см. логи).");
            }

            return new XrayRestartResult(true, StatusCodes.Status204NoContent, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task WriteAtomicAsync(string path, string content, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tmp = Path.Combine(
            directory ?? ".",
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        await File.WriteAllTextAsync(tmp, content, cancellationToken).ConfigureAwait(false);
        File.Move(tmp, fullPath, overwrite: true);
    }

    private async Task<bool> RunRestartCommandAsync(string command, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(TimeSpan.FromSeconds(60));

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        try
        {
            using var proc = Process.Start(psi);
            if (proc is null)
            {
                _logger.LogError("Failed to start restart process");
                return false;
            }

            await proc.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            var err = await proc.StandardError.ReadToEndAsync(linked.Token).ConfigureAwait(false);
            var stdout = await proc.StandardOutput.ReadToEndAsync(linked.Token).ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                _logger.LogError(
                    "Restart command exited with {Code}. stderr: {Err} stdout: {Out}",
                    proc.ExitCode,
                    err,
                    stdout);
                return false;
            }

            _logger.LogInformation("Restart command completed: {Command}", command);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restart command failed: {Command}", command);
            return false;
        }
    }
}
