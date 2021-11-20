#region license
//Original Source Code: https://github.com/abpframework/abp 

//Permissions of this copyleft license are conditioned on making available
//complete source code of licensed works and modifications under the same license
//or the GNU GPLv3. Copyright and license notices must be preserved.
//Contributors provide an express grant of patent rights. However, a larger
//work using the licensed work through interfaces provided by the licensed
//work may be distributed under different terms and without source code for
//the larger work.
//You may obtain a copy of the License at 

//https://www.gnu.org/licenses/lgpl-3.0.en.html

//Unless required by applicable law or agreed to in writing, software 
//distributed under the License is distributed on an "AS IS" BASIS, 
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//See the License for the specific language governing permissions and 
//limitations under the License. 
#endregion

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;
using RCommon.Json;

namespace RCommon.BackgroundJobs.Quartz
{
    public class QuartzBackgroundJobManager : IBackgroundJobManager
    {
        public const string JobDataPrefix = "Abp";
        public const string RetryIndex = "RetryIndex";

        protected IScheduler Scheduler { get; }

        protected BackgroundJobQuartzOptions Options { get; }

        protected IJsonSerializer JsonSerializer { get; }

        public QuartzBackgroundJobManager(IScheduler scheduler, IOptions<BackgroundJobQuartzOptions> options, IJsonSerializer jsonSerializer)
        {
            Scheduler = scheduler;
            JsonSerializer = jsonSerializer;
            Options = options.Value;
        }

        public virtual async Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal,
            TimeSpan? delay = null)
        {
            return await ReEnqueueAsync(args, Options.RetryCount, Options.RetryIntervalMillisecond, priority, delay);
        }

        public virtual async Task<string> ReEnqueueAsync<TArgs>(TArgs args, int retryCount, int retryIntervalMillisecond,
            BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            var jobDataMap = new JobDataMap
            {
                {nameof(TArgs), JsonSerializer.Serialize(args)},
                {JobDataPrefix+ nameof(Options.RetryCount), retryCount.ToString()},
                {JobDataPrefix+ nameof(Options.RetryIntervalMillisecond), retryIntervalMillisecond.ToString()},
                {JobDataPrefix+ RetryIndex, "0"}
            };

            var jobDetail = JobBuilder.Create<QuartzJobExecutionAdapter<TArgs>>().RequestRecovery().SetJobData(jobDataMap).Build();
            var trigger = !delay.HasValue ? TriggerBuilder.Create().StartNow().Build() : TriggerBuilder.Create().StartAt(new DateTimeOffset(DateTime.Now.Add(delay.Value))).Build();
            await Scheduler.ScheduleJob(jobDetail, trigger);
            return jobDetail.Key.ToString();
        }
    }
}
