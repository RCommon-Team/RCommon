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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon.ExceptionHandling;

namespace RCommon.BackgroundJobs
{
    public class BackgroundJobExecuter : RCommonService, IBackgroundJobExecuter
    {
        private readonly IExceptionManager _exceptionManager;

        protected BackgroundJobOptions Options { get; }

        public BackgroundJobExecuter(IOptions<BackgroundJobOptions> options, ILogger<BackgroundJobExecuter> logger, IExceptionManager exceptionManager)
            :base(logger)
        {
            Options = options.Value;
            _exceptionManager = exceptionManager;
        }

        public virtual async Task ExecuteAsync(JobExecutionContext context)
        {
            var job = context.ServiceProvider.GetService(context.JobType);
            if (job == null)
            {
                throw new GeneralException("The job type is not registered to DI: " + context.JobType);
            }

            var jobExecuteMethod = context.JobType.GetMethod(nameof(IBackgroundJob<object>.Execute)) ?? 
                                   context.JobType.GetMethod(nameof(IAsyncBackgroundJob<object>.ExecuteAsync));
            if (jobExecuteMethod == null)
            {
                throw new GeneralException($"Given job type does not implement {typeof(IBackgroundJob<>).Name} or {typeof(IAsyncBackgroundJob<>).Name}. " +
                                       "The job type was: " + context.JobType);
            }

            try
            {
                if (jobExecuteMethod.Name == nameof(IAsyncBackgroundJob<object>.ExecuteAsync))
                {
                    await ((Task) jobExecuteMethod.Invoke(job, new[] {context.JobArgs}));
                }
                else
                {
                    jobExecuteMethod.Invoke(job, new[] { context.JobArgs });
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "A background job execution is failed. See inner exception for details.";
                Logger.LogError(ex, errorMessage, context);

                _exceptionManager.HandleException(ex, DefaultExceptionPolicies.BasePolicy);

                throw new BackgroundJobExecutionException("A background job execution is failed. See inner exception for details.", ex)
                {
                    JobType = context.JobType.AssemblyQualifiedName,
                    JobArgs = context.JobArgs
                };
            }
        }
    }
}
