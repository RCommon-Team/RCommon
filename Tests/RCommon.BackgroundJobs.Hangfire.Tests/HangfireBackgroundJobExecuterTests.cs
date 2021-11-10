using NUnit.Framework;
using RCommon.BackgroundJobs;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace RCommon.BackgroundJobs.Hangfire.Tests
{
    public class HangfireBackgroundJobExecuterTests : BackgroundJobTestBase
    {
        private readonly IBackgroundJobExecuter _backgroundJobExecuter;

        public HangfireBackgroundJobExecuterTests()
        {
            _backgroundJobExecuter = this.ServiceProvider.GetService<IBackgroundJobExecuter>();
        }

        [Test]
        public async Task Should_Execute_Tasks()
        {
            //Arrange

            var jobObject = new MyJob();
            jobObject.ExecutedValues.ShouldBeEmpty();

            //Act

            await _backgroundJobExecuter.ExecuteAsync(
                new JobExecutionContext(
                    ServiceProvider,
                    typeof(MyJob),
                    new MyJobArgs("42")
                )
            );

            //Assert

            jobObject.ExecutedValues.ShouldContain("42");
        }

        [Test]
        public async Task Should_Execute_Async_Tasks()
        {
            //Arrange

            var jobObject = new MyAsyncJob();
            jobObject.ExecutedValues.ShouldBeEmpty();

            //Act

            await _backgroundJobExecuter.ExecuteAsync(
                new JobExecutionContext(
                    ServiceProvider,
                    typeof(MyAsyncJob),
                    new MyAsyncJobArgs("42")
                )
            );

            //Assert

            jobObject.ExecutedValues.ShouldContain("42");
        }
    }
}