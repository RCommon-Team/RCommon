using Microsoft.Extensions.Configuration;

namespace Examples.Messaging.SubscriptionIsolation
{
    internal static class ConfigurationContainer
    {
        public static IConfiguration Configuration { get; set; } = null!;
    }
}
