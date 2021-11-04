using System;
using System.Threading.Tasks;
using Hangfire;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class HangfireBackgroundJobManager : IBackgroundJobManager
    {
        public virtual Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal,
            TimeSpan? delay = null)
        {
            return Task.FromResult(delay.HasValue
                ? BackgroundJob.Schedule<HangfireJobExecutionAdapter<TArgs>>(
                    adapter => adapter.ExecuteAsync(args),
                    delay.Value
                )
                : BackgroundJob.Enqueue<HangfireJobExecutionAdapter<TArgs>>(
                    adapter => adapter.ExecuteAsync(args)
                ));
        }
    }
}
