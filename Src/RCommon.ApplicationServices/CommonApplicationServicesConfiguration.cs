using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Common;
using RCommon.BusinessServices;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ApplicationServices
{
    public class CommonApplicationServicesConfiguration : RCommonConfiguration, ICommonApplicationServicesConfiguration
    {
        public CommonApplicationServicesConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }

        public ICommonApplicationServicesConfiguration WithCrudHelpers()
        {
            this.ContainerAdapter.AddGeneric(typeof(ICrudBusinessService<>), typeof(CrudBusinessService<>));
            this.ContainerAdapter.AddGeneric(typeof(ICrudAppService<,>), typeof(CrudAppService<,>));
            return this;
        }

        public ICommonApplicationServicesConfiguration WithSmtpEmailServices(Func<EmailSettings, bool> emailSettings)
        {
            this.ContainerAdapter.AddTransient<IEmailService, EmailService>();
            return this;
        }

    }
}
