using System.Linq;
using Hangfire;
using Microsoft.Extensions.Options;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class DashboardOptionsProvider
    {
        protected BackgroundJobOptions BackgroundJobOptions { get; }

        public DashboardOptionsProvider(IOptions<BackgroundJobOptions> abpBackgroundJobOptions)
        {
            BackgroundJobOptions = abpBackgroundJobOptions.Value;
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
