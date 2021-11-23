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
using JetBrains.Annotations;
using Quartz;
using RCommon.Extensions;

namespace RCommon.BackgroundJobs.Quartz
{
    public class BackgroundJobQuartzOptions
    {
        public int RetryCount { get; set; }

        public int RetryIntervalMillisecond { get; set; }


        [NotNull]
        public Func<int, IJobExecutionContext, JobExecutionException,Task> RetryStrategy
        {
            get => _retryStrategy;
            set
            {
                Guard.IsNotNull(value, nameof(value));
                _retryStrategy = value;
            } 
        }
        private Func<int, IJobExecutionContext, JobExecutionException,Task> _retryStrategy;

        public BackgroundJobQuartzOptions()
        {
            RetryCount = 3;
            RetryIntervalMillisecond = 3000;
            _retryStrategy = DefaultRetryStrategy;
        }

        private async Task DefaultRetryStrategy(int retryIndex, IJobExecutionContext executionContext, JobExecutionException exception)
        {
            exception.RefireImmediately = true;

            var retryCount = executionContext.JobDetail.JobDataMap.GetString(QuartzBackgroundJobManager.JobDataPrefix+ nameof(RetryCount)).To<int>();
            if (retryIndex > retryCount)
            {
                exception.RefireImmediately = false;
                exception.UnscheduleAllTriggers = true;
                return;
            }

            var retryInterval = executionContext.JobDetail.JobDataMap.GetString(QuartzBackgroundJobManager.JobDataPrefix+ nameof(RetryIntervalMillisecond)).To<int>();
            await Task.Delay(retryInterval);
        }
    }
}
