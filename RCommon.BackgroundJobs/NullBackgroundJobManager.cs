using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RCommon.BackgroundJobs
{
    public class NullBackgroundJobManager : IBackgroundJobManager
    {
        public ILogger<NullBackgroundJobManager> Logger { get; set; }

        public NullBackgroundJobManager()
        {
            Logger = NullLogger<NullBackgroundJobManager>.Instance;
        }

        public virtual Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal,
            TimeSpan? delay = null)
        {
            throw new GeneralException("Background job system has not a real implementation. If it's mandatory, use an implementation (either the default provider or a 3rd party implementation). If it's optional, check IBackgroundJobManager.IsAvailable() extension method and act based on it.");
        }
    }
}