# RCommon.SendGrid

A SendGrid implementation of the RCommon `IEmailService` abstraction, enabling email delivery through the SendGrid API using standard `System.Net.Mail.MailMessage` objects.

## Features

- Implements `IEmailService` using the SendGrid API client
- Accepts standard `MailMessage` objects and converts them to SendGrid messages automatically
- Supports HTML and plain text email bodies
- Streams file attachments to the SendGrid API
- Supports sending to multiple recipients in a single call
- `EmailSent` event for post-send notifications
- API key configuration via the options pattern
- Fluent DI registration through the `AddRCommon()` builder

## Installation

```shell
dotnet add package RCommon.SendGrid
```

## Usage

```csharp
using RCommon;

services.AddRCommon()
    .WithSendGridEmailServices(settings =>
    {
        settings.SendGridApiKey = "your-sendgrid-api-key";
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
| `SendGridEmailService` | `IEmailService` implementation that sends email via the SendGrid API |
| `SendGridEmailSettings` | Configuration for the SendGrid API key and default sender details |
| `SendGridEmailingConfigurationExtensions` | Provides `WithSendGridEmailServices()` for the RCommon builder |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Emailing](https://www.nuget.org/packages/RCommon.Emailing) - Core email abstraction with SMTP implementation
- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
