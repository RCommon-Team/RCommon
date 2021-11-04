using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class HangfireJobExecutionAdapter<TArgs>
    {
        protected BackgroundJobOptions Options { get; }
        protected IServiceScopeFactory ServiceScopeFactory { get; }
        protected IBackgroundJobExecuter JobExecuter { get; }

        public HangfireJobExecutionAdapter(
            IOptions<BackgroundJobOptions> options,
            IBackgroundJobExecuter jobExecuter,
            IServiceScopeFactory serviceScopeFactory)
        {
            JobExecuter = jobExecuter;
            ServiceScopeFactory = serviceScopeFactory;
            Options = options.Value;
        }

        public async Task ExecuteAsync(TArgs args)
        {
            if (!Options.IsJobExecutionEnabled)
            {
                throw new GeneralException(
                    "Background job execution is disabled. " +
                    "This method should not be called! " +
                    "If you want to enable the background job execution, " +
                    $"set {nameof(BackgroundJobOptions)}.{nameof(BackgroundJobOptions.IsJobExecutionEnabled)} to true! " +
                    "If you've intentionally disabled job execution and this seems a bug, please report it."
                );
            }

            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var jobType = Options.GetJob(typeof(TArgs)).JobType;
                var context = new JobExecutionContext(scope.ServiceProvider, jobType, args);
                await JobExecuter.ExecuteAsync(context);
            }
        }
    }
}
