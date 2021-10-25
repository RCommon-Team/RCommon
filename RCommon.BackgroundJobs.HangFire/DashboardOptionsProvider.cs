using System.Linq;
using Hangfire;
using Microsoft.Extensions.Options;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class DashboardOptionsProvider
    {
        protected BackgroundJobOptions AbpBackgroundJobOptions { get; }

        public DashboardOptionsProvider(IOptions<BackgroundJobOptions> abpBackgroundJobOptions)
        {
            AbpBackgroundJobOptions = abpBackgroundJobOptions.Value;
        }

        public virtual DashboardOptions Get()
        {
            return new DashboardOptions
            {
                DisplayNameFunc = (dashboardContext, job) =>
                    BackgroundJobOptions.GetJob(job.Args.First().GetType()).JobName
            };
        }
    }
}
