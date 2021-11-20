using RCommon.ApplicationServices.Common;
using RCommon.Configuration;
using System;

namespace RCommon.ApplicationServices
{
    public interface ICommonApplicationServicesConfiguration : IServiceConfiguration
    {
        ICommonApplicationServicesConfiguration WithCrudHelpers();
        ICommonApplicationServicesConfiguration WithSmtpEmailServices(Action<EmailSettings> emailSettings);
    }
}
