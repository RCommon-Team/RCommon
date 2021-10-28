using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace RCommon.BackgroundJobs.Tests
{
    public class BackgroundJobManagerTests : BackgroundJobTestBase
    {
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly IBackgroundJobStore _backgroundJobStore;

        public BackgroundJobManagerTests()
        {
            _backgroundJobManager = GetRequiredService<IBackgroundJobManager>();
            _backgroundJobStore = GetRequiredService<IBackgroundJobStore>();
        }

        [Fact]
        public async Task Should_Store_Jobs()
        {
            var jobIdAsString = await _backgroundJobManager.EnqueueAsync(new MyJobArgs("42"));
            jobIdAsString.ShouldNotBe(default);
            (await _backgroundJobStore.FindAsync(Guid.Parse(jobIdAsString))).ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Store_Async_Jobs()
        {
            var jobIdAsString = await _backgroundJobManager.EnqueueAsync(new MyAsyncJobArgs("42"));
            jobIdAsString.ShouldNotBe(default);
            (await _backgroundJobStore.FindAsync(Guid.Parse(jobIdAsString))).ShouldNotBeNull();
        }
    }
}
