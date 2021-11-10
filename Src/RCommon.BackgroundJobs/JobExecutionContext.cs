using System;

namespace RCommon.BackgroundJobs
{
    public class JobExecutionContext
    {
        public IServiceProvider ServiceProvider { get; }

        public Type JobType { get; }

        public object JobArgs { get; }

        public JobExecutionContext(IServiceProvider serviceProvider, Type jobType, object jobArgs)
        {
            ServiceProvider = serviceProvider;
            JobType = jobType;
            JobArgs = jobArgs;
        }
    }
}