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

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RCommon.BackgroundJobs.Hangfire
{
    public class HangfireJobExecutionAdapter<TArgs>
    {
        protected BackgroundJobOptions Options { get; }
        protected IServiceScopeFactory ServiceScopeFactory { get; }
        protected IBackgroundJobExecuter JobExecuter { get; }

        public HangfireJobExecutionAdapter(
            IOptions<BackgroundJobOptions> options,
            IBackgroundJobExecuter jobExecuter,
            IServiceScopeFactory serviceScopeFactory)
        {
            JobExecuter = jobExecuter;
            ServiceScopeFactory = serviceScopeFactory;
            Options = options.Value;
        }

        public async Task ExecuteAsync(TArgs args)
        {
            if (!Options.IsJobExecutionEnabled)
            {
                throw new GeneralException(
                    "Background job execution is disabled. " +
                    "This method should not be called! " +
                    "If you want to enable the background job execution, " +
                    $"set {nameof(BackgroundJobOptions)}.{nameof(BackgroundJobOptions.IsJobExecutionEnabled)} to true! " +
                    "If you've intentionally disabled job execution and this seems a bug, please report it."
                );
            }

            using (var scope = ServiceScopeFactory.CreateScope())
            {
                var jobType = Options.GetJob(typeof(TArgs)).JobType;
                var context = new JobExecutionContext(scope.ServiceProvider, jobType, args);
                await JobExecuter.ExecuteAsync(context);
            }
        }
    }
}
