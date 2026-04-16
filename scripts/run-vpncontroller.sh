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
[[ -z "${Subscriptions__BaseUrl:-}" && -n "${SUBSCRIPTIONS_BASE_URL:-}" ]] && export Subscriptions__BaseUrl="$SUBSCRIPTIONS_BASE_URL"
[[ -z "${Subscriptions__SubscriptionGuid:-}" && -n "${SUBSCRIPTIONS_SUBSCRIPTION_GUID:-}" ]] && export Subscriptions__SubscriptionGuid="$SUBSCRIPTIONS_SUBSCRIPTION_GUID"
[[ -z "${Subscriptions__RefreshIntervalSeconds:-}" && -n "${SUBSCRIPTIONS_REFRESH_INTERVAL_SECONDS:-}" ]] && export Subscriptions__RefreshIntervalSeconds="$SUBSCRIPTIONS_REFRESH_INTERVAL_SECONDS"
[[ -z "${Database__SqlitePath:-}" && -n "${DATABASE_SQLITE_PATH:-}" ]] && export Database__SqlitePath="$DATABASE_SQLITE_PATH"
[[ -z "${Xray__LogLevel:-}" && -n "${XRAY_LOG_LEVEL:-}" ]] && export Xray__LogLevel="$XRAY_LOG_LEVEL"
[[ -z "${Xray__PublicHost:-}" && -n "${XRAY_PUBLIC_HOST:-}" ]] && export Xray__PublicHost="$XRAY_PUBLIC_HOST"
[[ -z "${Xray__InboundShared__Dest:-}" && -n "${XRAY_INBOUND_SHARED_DEST:-}" ]] && export Xray__InboundShared__Dest="$XRAY_INBOUND_SHARED_DEST"
[[ -z "${Xray__InboundShared__PrivateKey:-}" && -n "${XRAY_INBOUND_SHARED_PRIVATE_KEY:-}" ]] && export Xray__InboundShared__PrivateKey="$XRAY_INBOUND_SHARED_PRIVATE_KEY"
[[ -z "${Xray__InboundShared__PublicKey:-}" && -n "${XRAY_INBOUND_SHARED_PUBLIC_KEY:-}" ]] && export Xray__InboundShared__PublicKey="$XRAY_INBOUND_SHARED_PUBLIC_KEY"
[[ -z "${Xray__InboundShortIdsCsv:-}" && -n "${XRAY_INBOUND_SHORT_IDS:-}" ]] && export Xray__InboundShortIdsCsv="$XRAY_INBOUND_SHORT_IDS"
[[ -z "${Xray__Restart__Enabled:-}" && -n "${XRAY_RESTART_ENABLED:-}" ]] && export Xray__Restart__Enabled="$XRAY_RESTART_ENABLED"
[[ -z "${Xray__Restart__ConfigFilePath:-}" && -n "${XRAY_RESTART_CONFIG_PATH:-}" ]] && export Xray__Restart__ConfigFilePath="$XRAY_RESTART_CONFIG_PATH"
[[ -z "${Xray__Restart__RestartCommand:-}" && -n "${XRAY_RESTART_COMMAND:-}" ]] && export Xray__Restart__RestartCommand="$XRAY_RESTART_COMMAND"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
# По умолчанию только localhost — порт не пересекается с инбаундами xray (8080–8089).
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://127.0.0.1:5000}"

if [[ -n "${VPNCONTROLLER_PUBLISH_DIR:-}" ]]; then
  exec dotnet "$VPNCONTROLLER_PUBLISH_DIR/VpnController.dll" "$@"
fi

exec dotnet run --project "$ROOT/VpnController/VpnController.csproj" --no-launch-profile "$@"
