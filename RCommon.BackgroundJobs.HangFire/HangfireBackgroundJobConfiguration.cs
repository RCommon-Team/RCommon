using RCommon.BackgroundJobs.Hangfire;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class HangFireBackgroundJobConfiguration : RCommonConfiguration
    {

        public HangFireBackgroundJobConfiguration(IContainerAdapter containerAdapter) : base(containerAdapter)
        {

        }
    }
}
