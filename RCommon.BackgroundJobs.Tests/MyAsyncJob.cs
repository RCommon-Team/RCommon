using RCommon.BackgroundJobs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.BackgroundJobs
{
    public class MyAsyncJob : AsyncBackgroundJob<MyAsyncJobArgs>
    {
        public List<string> ExecutedValues { get; } = new List<string>();

        public override Task ExecuteAsync(MyAsyncJobArgs args)
        {
            ExecutedValues.Add(args.Value);

            return Task.CompletedTask;
        }
    }
}
