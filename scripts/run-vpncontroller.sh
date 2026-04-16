#!/usr/bin/env bash
# Запуск VpnController на хосте (systemctl restart xray, запись /usr/local/etc/xray/config.json).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ -f "$ROOT/.env" ]]; then
  set -a
  # shellcheck source=/dev/null
  source "$ROOT/.env"
  set +a
fi

# Имена как в docker-compose → ключи ASP.NET (если уже не заданы явно)
[[ -z "${SubscriptionRefreshOptions__SubscriptionUrl:-}" && -n "${SUBSCRIPTIONS_BASE_URL:-}" ]] && export SubscriptionRefreshOptions__SubscriptionUrl="$SUBSCRIPTIONS_BASE_URL"
[[ -z "${SubscriptionRefreshOptions__RefreshIntervalSeconds:-}" && -n "${SUBSCRIPTIONS_REFRESH_INTERVAL_SECONDS:-}" ]] && export SubscriptionRefreshOptions__RefreshIntervalSeconds="$SUBSCRIPTIONS_REFRESH_INTERVAL_SECONDS"
[[ -z "${DatabaseOptions__SqlitePath:-}" && -n "${DATABASE_SQLITE_PATH:-}" ]] && export DatabaseOptions__SqlitePath="$DATABASE_SQLITE_PATH"
[[ -z "${XrayCoreOptions__LogLevel:-}" && -n "${XRAY_LOG_LEVEL:-}" ]] && export XrayCoreOptions__LogLevel="$XRAY_LOG_LEVEL"
[[ -z "${XrayCoreOptions__PublicHost:-}" && -n "${XRAY_PUBLIC_HOST:-}" ]] && export XrayCoreOptions__PublicHost="$XRAY_PUBLIC_HOST"
[[ -z "${XrayCoreOptions__InboundShared__Dest:-}" && -n "${XRAY_INBOUND_SHARED_DEST:-}" ]] && export XrayCoreOptions__InboundShared__Dest="$XRAY_INBOUND_SHARED_DEST"
[[ -z "${XrayCoreOptions__InboundShared__PrivateKey:-}" && -n "${XRAY_INBOUND_SHARED_PRIVATE_KEY:-}" ]] && export XrayCoreOptions__InboundShared__PrivateKey="$XRAY_INBOUND_SHARED_PRIVATE_KEY"
[[ -z "${XrayCoreOptions__InboundShared__PublicKey:-}" && -n "${XRAY_INBOUND_SHARED_PUBLIC_KEY:-}" ]] && export XrayCoreOptions__InboundShared__PublicKey="$XRAY_INBOUND_SHARED_PUBLIC_KEY"
[[ -z "${XrayCoreOptions__InboundShortIdsCsv:-}" && -n "${XRAY_INBOUND_SHORT_IDS:-}" ]] && export XrayCoreOptions__InboundShortIdsCsv="$XRAY_INBOUND_SHORT_IDS"
[[ -z "${XrayCoreOptions__Restart__Enabled:-}" && -n "${XRAY_RESTART_ENABLED:-}" ]] && export XrayCoreOptions__Restart__Enabled="$XRAY_RESTART_ENABLED"
[[ -z "${XrayCoreOptions__Restart__ConfigFilePath:-}" && -n "${XRAY_RESTART_CONFIG_PATH:-}" ]] && export XrayCoreOptions__Restart__ConfigFilePath="$XRAY_RESTART_CONFIG_PATH"
[[ -z "${XrayCoreOptions__Restart__RestartCommand:-}" && -n "${XRAY_RESTART_COMMAND:-}" ]] && export XrayCoreOptions__Restart__RestartCommand="$XRAY_RESTART_COMMAND"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
# По умолчанию только localhost — порт не пересекается с инбаундами xray (8080–8089).
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://127.0.0.1:5000}"

if [[ -n "${VPNCONTROLLER_PUBLISH_DIR:-}" ]]; then
  exec dotnet "$VPNCONTROLLER_PUBLISH_DIR/VpnController.dll" "$@"
fi

exec dotnet run --project "$ROOT/VpnController/VpnController.csproj" --no-launch-profile "$@"
