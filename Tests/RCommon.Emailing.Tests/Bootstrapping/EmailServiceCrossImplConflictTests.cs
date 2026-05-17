using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Emailing.SendGrid;
using RCommon.Emailing.Smtp;
using Xunit;

namespace RCommon.Emailing.Tests.Bootstrapping;

public class EmailServiceCrossImplConflictTests
{
    [Fact]
    public void WithSmtpThenWithSendGrid_Throws()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSmtpEmailServices(opts => { });

        Action act = () => builder.WithSendGridEmailServices(opts => { });

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SmtpEmailService*SendGridEmailService*");
    }

    [Fact]
    public void WithSendGridThenWithSmtp_Throws()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSendGridEmailServices(opts => { });

        Action act = () => builder.WithSmtpEmailServices(opts => { });

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SendGridEmailService*SmtpEmailService*");
    }
}
