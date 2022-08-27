using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using RCommon.Configuration;
using RCommon.DependencyInjection.Microsoft;
using RCommon.Emailing;
using RCommon.Emailing.SendGrid;
using RCommon.TestBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Tests.Application.Services
{
    [TestFixture]
    public class SmtpEmailServiceIntegration : TestBootstrapper
    {
        private bool _emailSent = false;
        public SmtpEmailServiceIntegration()
        {
            var services = new ServiceCollection();

            services.AddTransient<IEmailService, SendGridEmailService>();

            this.InitializeRCommon(services);



        }

        protected void InitializeRCommon(IServiceCollection services)
        {

            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .WithSendGridEmailServices(settings =>
                {
                    settings.SendGridApiKey = "apiKey";
                });



            this.ServiceProvider = services.BuildServiceProvider();
            this.Logger = this.ServiceProvider.GetService<ILogger>();

            Debug.WriteLine($"Total Services Registered: {services.Count}");
            foreach (var service in services)
            {
                Debug.WriteLine($"Service: {service.ServiceType.FullName}\n Lifetime: {service.Lifetime}\n Instance: {service.ImplementationType?.FullName}");
            }

            this.InitializeBootstrapper(services);

        }


        [Test]
        public void Can_send_email()
        {
            _emailSent = false;
            var message = new MailMessage();
            message.To.Add("youremail@test.com");
            message.From = new MailAddress("test@test.com");
            message.Subject = "Test Email";
            message.Body = "Test Body of Message";
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;


            var mock = new Mock<IEmailService>();
            mock.Setup(x => x.SendEmail(message))
                .Raises(e => e.EmailSent += null, new EmailEventArgs(message));
            mock.Object.EmailSent += EmailService_EmailSent;
            
            mock.Object.SendEmail(message);

            Assert.IsTrue(_emailSent);

        }

        private void EmailService_EmailSent(object sender, EmailEventArgs e)
        {
            Assert.IsNotNull(e);
            Assert.IsTrue(e.MailMessage.Subject == "Test Email");
            _emailSent = true;
        }

        [Test]
        public async Task Can_send_email_async()
        {
            _emailSent = false;
            var message = new MailMessage();
            message.To.Add("youremail@test.com");
            message.From = new MailAddress("test@test.com");
            message.Subject = "Test Email";
            message.Body = "Test Body of Message";
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;


            var mock = new Mock<IEmailService>();
            mock.Setup(x => x.SendEmailAsync(message))
                .Returns(Task.CompletedTask)
                .Raises(e => e.EmailSent += null, new EmailEventArgs(message));
            mock.Object.EmailSent += EmailService_EmailSent;

            await mock.Object.SendEmailAsync(message);

            Assert.IsTrue(_emailSent);
        }

    }
}
