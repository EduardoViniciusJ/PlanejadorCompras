using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace PlanejadorCompras.API.Configuration;

internal static class ApiProxyConfiguration
{
    internal static IServiceCollection AddApiProxySupport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var value in configuration.GetSection("Proxy:KnownProxies").Get<string[]>()
                         ?? Array.Empty<string>())
            {
                if (!IPAddress.TryParse(value, out var address))
                {
                    throw new InvalidOperationException(
                        $"Invalid IP address in 'Proxy:KnownProxies': '{value}'.");
                }

                options.KnownProxies.Add(address);
            }

            // Cloud load balancers rarely have a fixed address: a CIDR range can be trusted instead.
            foreach (var value in configuration.GetSection("Proxy:KnownNetworks").Get<string[]>()
                         ?? Array.Empty<string>())
            {
                if (!System.Net.IPNetwork.TryParse(value, out var network))
                {
                    throw new InvalidOperationException(
                        $"Invalid CIDR range in 'Proxy:KnownNetworks': '{value}'.");
                }

                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }
}
