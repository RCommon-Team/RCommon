using NUnit.Framework;
using RCommon.BackgroundJobs;
using RCommon.BackgroundJobs.Tests;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;


namespace RCommon.BackgroundJobs.Tests
{
    public class BackgroundJobExecuterTests : BackgroundJobTestBase
    {
        private readonly IBackgroundJobExecuter _backgroundJobExecuter;

        public BackgroundJobExecuterTests()
        {
            _backgroundJobExecuter = this.ServiceProvider.GetService<IBackgroundJobExecuter>();
        }

        [Test]
        public async Task Should_Execute_Tasks()
        {
            //Arrange

            var jobObject = this.ServiceProvider.GetService<MyJob>();
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