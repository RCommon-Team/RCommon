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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;
using RCommon.Extensions;
using RCommon.Serialization.Json;

namespace RCommon.BackgroundJobs.Quartz
{
    public class QuartzJobExecutionAdapter<TArgs> : IJob
    {
        public ILogger<QuartzJobExecutionAdapter<TArgs>> Logger { get; set; }

        protected BackgroundJobOptions Options { get; }
        protected BackgroundJobQuartzOptions BackgroundJobQuartzOptions { get; }
        protected IServiceScopeFactory ServiceScopeFactory { get; }
        protected IBackgroundJobExecuter JobExecuter { get; }
        protected IJsonSerializer JsonSerializer { get; }

        public QuartzJobExecutionAdapter(
            IOptions<BackgroundJobOptions> options,
            IOptions<BackgroundJobQuartzOptions> backgroundJobQuartzOptions,
            IBackgroundJobExecuter jobExecuter,
            IServiceScopeFactory serviceScopeFactory,
            IJsonSerializer jsonSerializer)
        {
            JobExecuter = jobExecuter;
            ServiceScopeFactory = serviceScopeFactory;
            JsonSerializer = jsonSerializer;
            Options = options.Value;
            BackgroundJobQuartzOptions = backgroundJobQuartzOptions.Value;
            Logger = NullLogger<QuartzJobExecutionAdapter<TArgs>>.Instance;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var args =  JsonSerializer.Deserialize<TArgs>(context.JobDetail.JobDataMap.GetString(nameof(TArgs)));
                var jobType = Options.GetJob(typeof(TArgs)).JobType;
                var jobContext = new JobExecutionContext(scope.ServiceProvider, jobType, args);
                try
                {
                    await JobExecuter.ExecuteAsync(jobContext);
                }
                catch (Exception exception)
                {
                    var jobExecutionException = new JobExecutionException(exception);

                    var retryIndex = context.JobDetail.JobDataMap.GetString(QuartzBackgroundJobManager.JobDataPrefix+ QuartzBackgroundJobManager.RetryIndex).To<int>();
                    retryIndex++;
                    context.JobDetail.JobDataMap.Put(QuartzBackgroundJobManager.JobDataPrefix+ QuartzBackgroundJobManager.RetryIndex, retryIndex.ToString());

                    await BackgroundJobQuartzOptions.RetryStrategy.Invoke(retryIndex, context, jobExecutionException);

                    throw jobExecutionException;
                }
            }
        }
    }
}
