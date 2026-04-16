using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace VpnController.Services;

/// <summary>
/// Собирает JSON конфига Xray: клиенты из БД дублируются во всех инбаундах;
/// строки подписки (первые 9 vless) — в outbounds sota-01 … sota-09 по порядку.
/// </summary>
public sealed class XrayConfigGenerator
{
    private readonly XrayCoreOptions _options;

    public XrayConfigGenerator(IOptions<XrayCoreOptions> options)
    {
        _options = options.Value;
    }

    public JsonObject Build(IReadOnlyList<Guid> userIds, IReadOnlyList<ParsedVlessConnection> upstreams)
    {
        var shared = _options.InboundShared;

        var inbounds = new JsonArray();
        foreach (var ib in _options.Inbounds)
        {
            // Новый JsonArray на каждый inbound: один и тот же JsonNode нельзя вешать в несколько родителей.
            inbounds.Add(BuildInbound(ib, userIds, shared));
        }

        var outbounds = new JsonArray
        {
            new JsonObject
            {
                ["tag"] = "direct",
                ["protocol"] = "freedom"
            }
        };

        for (var i = 0; i < upstreams.Count; i++)
        {
            var tag = $"sota-{i + 1:D2}";
            outbounds.Add(BuildVlessOutbound(tag, upstreams[i]));
        }

        var rules = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "field",
                ["inboundTag"] = new JsonArray("direct-in"),
                ["outboundTag"] = "direct"
            }
        };

        for (var i = 0; i < upstreams.Count; i++)
        {
            var inboundTag = $"sota-{i + 1:D2}-in";
            var outboundTag = $"sota-{i + 1:D2}";
            rules.Add(new JsonObject
            {
                ["type"] = "field",
                ["inboundTag"] = new JsonArray(inboundTag),
                ["outboundTag"] = outboundTag
            });
        }

        return new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = _options.LogLevel },
            ["inbounds"] = inbounds,
            ["outbounds"] = outbounds,
            ["routing"] = new JsonObject { ["rules"] = rules }
        };
    }

    private static JsonArray BuildClientsArray(IReadOnlyList<Guid> userIds)
    {
        var clients = new JsonArray();
        foreach (var id in userIds)
        {
            clients.Add(new JsonObject
            {
                ["id"] = id.ToString("D"),
                ["flow"] = "xtls-rprx-vision"
            });
        }

        return clients;
    }

    private static JsonObject BuildInbound(
        InboundBindingOptions ib,
        IReadOnlyList<Guid> userIds,
        InboundSharedOptions shared)
    {
        var clients = BuildClientsArray(userIds);

        var serverNames = new JsonArray();
        foreach (var name in shared.ServerNames)
        {
            serverNames.Add(name);
        }

        var shortIds = new JsonArray();
        foreach (var sid in ib.ShortIds)
        {
            shortIds.Add(sid);
        }

        return new JsonObject
        {
            ["tag"] = ib.Tag,
            ["port"] = ib.Port,
            ["protocol"] = "vless",
            ["settings"] = new JsonObject
            {
                ["clients"] = clients,
                ["decryption"] = "none"
            },
            ["streamSettings"] = new JsonObject
            {
                ["network"] = "tcp",
                ["security"] = "reality",
                ["realitySettings"] = new JsonObject
                {
                    ["dest"] = shared.Dest,
                    ["serverNames"] = serverNames,
                    ["privateKey"] = shared.PrivateKey,
                    ["shortIds"] = shortIds
                }
            }
        };
    }

    private static JsonObject BuildVlessOutbound(string tag, ParsedVlessConnection c)
    {
        var flow = string.IsNullOrEmpty(c.Flow) ? "xtls-rprx-vision" : c.Flow;
        return new JsonObject
        {
            ["tag"] = tag,
            ["protocol"] = "vless",
            ["settings"] = new JsonObject
            {
                ["vnext"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["address"] = c.Host,
                        ["port"] = c.Port,
                        ["users"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["id"] = c.UserId,
                                ["encryption"] = "none",
                                ["flow"] = flow
                            }
                        }
                    }
                }
            },
            ["streamSettings"] = new JsonObject
            {
                ["network"] = "tcp",
                ["security"] = "reality",
                ["realitySettings"] = new JsonObject
                {
                    ["serverName"] = c.ServerName ?? "",
                    ["publicKey"] = c.PublicKey ?? "",
                    ["shortId"] = c.ShortId ?? "",
                    ["fingerprint"] = string.IsNullOrEmpty(c.Fingerprint) ? "chrome" : c.Fingerprint
                }
            }
        };
    }

    public static string ToIndentedJson(JsonObject root)
    {
        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
