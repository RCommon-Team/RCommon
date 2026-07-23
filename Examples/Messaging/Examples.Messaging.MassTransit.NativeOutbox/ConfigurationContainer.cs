using Microsoft.Extensions.Configuration;

namespace Examples.Messaging.MassTransit.NativeOutbox;

internal static class ConfigurationContainer
{
    public static IConfiguration? Configuration { get; set; }
}
