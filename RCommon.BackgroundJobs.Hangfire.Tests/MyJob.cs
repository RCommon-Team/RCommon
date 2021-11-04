using RCommon.BackgroundJobs;
using System.Collections.Generic;

namespace RCommon.BackgroundJobs.Hangfire.Tests
{
    public class MyJob : BackgroundJob<MyJobArgs>
    {
        public List<string> ExecutedValues { get; } = new List<string>();

        public override void Execute(MyJobArgs args)
        {
            ExecutedValues.Add(args.Value);
        }
    }
}