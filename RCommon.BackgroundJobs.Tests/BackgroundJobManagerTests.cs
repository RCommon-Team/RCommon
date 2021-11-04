using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;

namespace RCommon.BackgroundJobs.Tests
{
    public class BackgroundJobManagerTests : BackgroundJobTestBase
    {
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly IBackgroundJobStore _backgroundJobStore;

        public BackgroundJobManagerTests()
        {
            _backgroundJobManager = this.ServiceProvider.GetService<IBackgroundJobManager>();
            _backgroundJobStore = this.ServiceProvider.GetService<IBackgroundJobStore>();
        }

        [Test]
        public async Task Should_Store_Jobs()
        {
            var jobIdAsString = await _backgroundJobManager.EnqueueAsync(new MyJobArgs("42"));
            jobIdAsString.ShouldNotBe(default);
            (await _backgroundJobStore.FindAsync(Guid.Parse(jobIdAsString))).ShouldNotBeNull();
        }

        [Test]
        public async Task Should_Store_Async_Jobs()
        {
            var jobIdAsString = await _backgroundJobManager.EnqueueAsync(new MyAsyncJobArgs("42"));
            jobIdAsString.ShouldNotBe(default);
            (await _backgroundJobStore.FindAsync(Guid.Parse(jobIdAsString))).ShouldNotBeNull();
        }
    }
}
