using System;

namespace RCommon.BackgroundJobs
{
    public class BackgroundJobSettings
    {
        public Type ArgsType { get; }

        public Type JobType { get; }

        public string JobName { get; }

        public BackgroundJobSettings(Type jobType)
        {
            JobType = jobType;
            ArgsType = BackgroundJobArgsHelper.GetJobArgsType(jobType);
            JobName = BackgroundJobNameAttribute.GetName(ArgsType);
        }
    }
}