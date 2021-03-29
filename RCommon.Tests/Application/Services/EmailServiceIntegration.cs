using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RCommon.ApplicationServices;
using RCommon.Configuration;
using RCommon.DependencyInjection.Microsoft;
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
    public class EmailServiceIntegration : TestBootstrapper
    {
        public EmailServiceIntegration()
        {
            var services = new ServiceCollection();

            services.AddTransient<IEmailService, EmailService>();

            this.InitializeRCommon(services);
            
        }

        protected void InitializeRCommon(IServiceCollection services)
        {


            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services))
                .And<CommonApplicationServicesConfiguration>();



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
            var settings = new EmailSettings();
            settings.EnableSsl = true;
            settings.From = "you@ytest.com";
            settings.Host = "smtp.sendgrid.net";
            settings.Password = "yourpassword";
            settings.Port = 587;
            settings.UserName = "apikey";
            var message = new MailMessage();
            message.To.Add("youremail@test.com");
            message.From = new MailAddress(settings.From);
            message.Subject = "Test Email";
            message.Body = "Test Body of Message";
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            var emailService = this.ServiceProvider.GetService<IEmailService>();
            emailService.SendEmail(message, settings);

            emailService.EmailSent += EmailService_EmailSent;

            Assert.Inconclusive("You must check email to ensure email was sent properly.");

        }

        private void EmailService_EmailSent(object sender, EventArgs e)
        {
            this.Logger.LogInformation("Emailer worked", null);
        }

        [Test]
        public async Task Can_send_email_async()
        {
            var settings = new EmailSettings();
            settings.EnableSsl = true;
            settings.From = "you@ytest.com";
            settings.Host = "smtp.sendgrid.net";
            settings.Password = "yourpassword";
            settings.Port = 587;
            settings.UserName = "apikey";
            var message = new MailMessage();
            message.To.Add("youremail@test.com");
            message.From = new MailAddress(settings.From);
            message.Subject = "Test Email";
            message.Body = "Test Body of Message";
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;

            var emailService = this.ServiceProvider.GetService<IEmailService>();
            await emailService.SendEmailAsync(message, settings);

            emailService.EmailSent += EmailService_EmailSent;

            Assert.Inconclusive("You must check email to ensure email was sent properly.");
        }

    }
}
