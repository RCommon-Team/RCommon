using System.Linq;
using Hangfire;
using Microsoft.Extensions.Options;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class HangfireDashboardOptionsProvider
    {
        protected BackgroundJobOptions BackgroundJobOptions { get; }

        public HangfireDashboardOptionsProvider(IOptions<BackgroundJobOptions> backgroundJobOptions)
        {
            BackgroundJobOptions = backgroundJobOptions.Value;
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
