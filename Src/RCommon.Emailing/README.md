# RCommon.Emailing

An email service abstraction for .NET with a built-in SMTP implementation, designed to integrate with the RCommon dependency injection builder pattern.

## Features

- `IEmailService` abstraction for sending email via `System.Net.Mail.MailMessage`
- Synchronous and asynchronous send methods
- Built-in SMTP implementation (`SmtpEmailService`) using `System.Net.Mail.SmtpClient`
- Configurable SMTP settings including host, port, SSL, credentials, and default sender
- `EmailSent` event for post-send notifications
- Fluent DI registration through the `AddRCommon()` builder

## Installation

```shell
dotnet add package RCommon.Emailing
```

## Usage

```csharp
using RCommon;

services.AddRCommon()
    .WithSmtpEmailServices(settings =>
    {
        settings.Host = "smtp.example.com";
        settings.Port = 587;
        settings.EnableSsl = true;
        settings.UserName = "user@example.com";
        settings.Password = "password";
        settings.FromEmailDefault = "noreply@example.com";
        settings.FromNameDefault = "My Application";
    });
```

Then inject and use `IEmailService`:

```csharp
using RCommon.Emailing;
using System.Net.Mail;

public class NotificationService
{
    private readonly IEmailService _emailService;

    public NotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendWelcomeEmailAsync(string toAddress)
    {
        var message = new MailMessage("noreply@example.com", toAddress)
        {
            Subject = "Welcome!",
            Body = "<h1>Welcome to our app</h1>",
            IsBodyHtml = true
        };

        await _emailService.SendEmailAsync(message);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `IEmailService` | Abstraction for sending email with sync and async methods |
| `SmtpEmailService` | SMTP-based implementation using `System.Net.Mail.SmtpClient` |
| `SmtpEmailSettings` | Configuration for SMTP host, port, SSL, credentials, and default sender |
| `EmailEventArgs` | Event args carrying the `MailMessage` after a successful send |
| `EmailingBuilderExtensions` | Provides `WithSmtpEmailServices()` for the RCommon builder |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.SendGrid](https://www.nuget.org/packages/RCommon.SendGrid) - SendGrid implementation of `IEmailService`
- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
