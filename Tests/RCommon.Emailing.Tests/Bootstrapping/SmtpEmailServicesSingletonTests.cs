using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing;
using RCommon.Emailing.Smtp;
using Xunit;

namespace RCommon.Emailing.Tests.Bootstrapping;

public class SmtpEmailServicesSingletonTests
{
    [Fact]
    public void WithSmtpEmailServices_CalledTwice_RegistersIEmailServiceOnce()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithSmtpEmailServices(opts => { });
        builder.WithSmtpEmailServices(opts => { });

        services.Count(d => d.ServiceType == typeof(IEmailService)).Should().Be(1);
    }
}
