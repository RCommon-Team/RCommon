using RCommon.ApplicationServices.Common;
using System;

namespace RCommon.ApplicationServices
{
    public interface ICommonApplicationServicesConfiguration
    {
        ICommonApplicationServicesConfiguration WithCrudHelpers();
        ICommonApplicationServicesConfiguration WithSmtpEmailServices(Func<EmailSettings, bool> emailSettings);
    }
}