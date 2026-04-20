using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using VpnController.Data;
using VpnController.Options;
using VpnController.Repositories;

namespace VpnController.Services;

public sealed class XrayConfigGenerator
{
    private readonly UserRepository _userRepository;
    private readonly SotaSubscriptionRepository _sotaSubscriptionRepository;

    private readonly XrayCoreOptions _options;

    public XrayConfigGenerator(
        UserRepository userRepository, 
        SotaSubscriptionRepository sotaSubscriptionRepository,
        IOptions<XrayCoreOptions> options)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _sotaSubscriptionRepository = sotaSubscriptionRepository ?? throw new ArgumentNullException(nameof(sotaSubscriptionRepository));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<JsonObject> Build()
    {
        var users = await _userRepository.GetAllAsync();
        var inbound = BuildMainInbound(users);
        
        var inbounds = new JsonArray { inbound };

        var outbounds = new JsonArray();

        var sotaVlessConnections = _sotaSubscriptionRepository.GetConnections();
        foreach (var sotaVlessConnection in sotaVlessConnections)
        {
            var tag = sotaVlessConnection.Name;
            outbounds.Add(BuildVlessOutbound(sotaVlessConnection));
        }

        var rules = BuildRoutingRules(users);

        return new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = _options.LogLevel },
            ["inbounds"] = inbounds,
            ["outbounds"] = outbounds,
            ["routing"] = new JsonObject { ["rules"] = rules }
        };
    }

    private JsonObject BuildMainInbound(IReadOnlyList<User> users)
    {
        var clients = new JsonArray();
        foreach (var user in users)
        {
            foreach (var id in user.ClientUuids)
            {
                clients.Add(new JsonObject
                {
                    ["id"] = id.ToString(),
                    ["flow"] = "xtls-rprx-vision"
                });
            }
        }
        
        return new JsonObject
        {
            ["tag"] = _options.MainInboundTag,
            ["port"] = _options.InboundPort,
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
                    ["dest"] = _options.Dest,
                    ["serverNames"] = _options.ServerName,
                    ["privateKey"] = _options.PrivateKey,
                    ["shortIds"] = _options.ShortId
                }
            }
        };
    }

    private JsonArray BuildRoutingRules(IReadOnlyList<User> clients)
    {
        var rules = new JsonArray();
        var sotaVlessConnections = _sotaSubscriptionRepository.GetConnections();


        for (var i = 0; i < sotaVlessConnections.Count; i++)
        {
            var sotaVlessConnection = sotaVlessConnections[i];
            foreach (var client in clients)
            {
                if (client.ClientUuids.Count != sotaVlessConnections.Count)
                {
                    throw new InvalidOperationException("SOTA changed, mapping broken");
                }
                
                var outboundTag = sotaVlessConnection.Name;

                rules.Add(new JsonObject
                {
                    ["type"] = "field",
                    ["user"] = new JsonArray(client.ClientUuids[i].ToString()),
                    ["outboundTag"] = outboundTag
                });
            }
        }

        return rules;
    }

    private JsonObject BuildVlessOutbound(SotaVlessConnection sotaInbound)
    {
        return new JsonObject
        {
            ["tag"] = sotaInbound.Name,
            ["protocol"] = "vless",
            ["settings"] = new JsonObject
            {
                ["vnext"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["address"] = sotaInbound.Host,
                        ["port"] = sotaInbound.Port,
                        ["users"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["id"] = sotaInbound.UserId,
                                ["encryption"] = "none",
                                ["flow"] = "xtls-rprx-vision"
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
                    ["serverName"] = sotaInbound.ServerName,
                    ["publicKey"] = sotaInbound.PublicKey,
                    ["shortId"] = sotaInbound.ShortId,
                    ["fingerprint"] = "safari"
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
