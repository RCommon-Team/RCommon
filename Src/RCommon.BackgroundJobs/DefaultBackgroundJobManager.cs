﻿using System;
using System.Threading.Tasks;

namespace RCommon.BackgroundJobs
{
    /// <summary>
    /// Default implementation of <see cref="IBackgroundJobManager"/>.
    /// </summary>
    public class DefaultBackgroundJobManager : IBackgroundJobManager
    {
        protected ISystemTime Clock { get; }
        protected IBackgroundJobSerializer Serializer { get; }
        protected IGuidGenerator GuidGenerator { get; }
        protected IBackgroundJobStore Store { get; }
        
        public DefaultBackgroundJobManager(
            ISystemTime clock,
            IBackgroundJobSerializer serializer,
            IBackgroundJobStore store,
            IGuidGenerator guidGenerator)
        {
            Clock = clock;
            Serializer = serializer;
            GuidGenerator = guidGenerator;
            Store = store;
        }

        public virtual async Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            var jobName = BackgroundJobNameAttribute.GetName<TArgs>();
            var jobId = await EnqueueAsync(jobName, args, priority, delay);
            return jobId.ToString();
        }

        protected virtual async Task<Guid> EnqueueAsync(string jobName, object args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            var jobInfo = new BackgroundJobInfo
            {
                Id = GuidGenerator.Create(),
                JobName = jobName,
                JobArgs = Serializer.Serialize(args),
                Priority = priority,
                CreationTime = Clock.Now,
                NextTryTime = Clock.Now
            };

            if (delay.HasValue)
            {
                jobInfo.NextTryTime = Clock.Now.Add(delay.Value);
            }

            await Store.InsertAsync(jobInfo);

            return jobInfo.Id;
        }
    }
}