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

namespace RCommon.BackgroundJobs
{
    /// <summary>
    /// Default implementation of <see cref="IBackgroundJobManager"/>.
    /// </summary>
    public class DefaultBackgroundJobManager : IBackgroundJobManager
    {
        protected ISystemTime Clock { get; }
        protected IBackgroundJobSerializer Serializer { get; }
        protected IGuidGenerator GuidGenerator { get; }
        protected IBackgroundJobStore Store { get; }
        
        public DefaultBackgroundJobManager(
            ISystemTime clock,
            IBackgroundJobSerializer serializer,
            IBackgroundJobStore store,
            IGuidGenerator guidGenerator)
        {
            Clock = clock;
            Serializer = serializer;
            GuidGenerator = guidGenerator;
            Store = store;
        }

        public virtual async Task<string> EnqueueAsync<TArgs>(TArgs args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            var jobName = BackgroundJobNameAttribute.GetName<TArgs>();
            var jobId = await EnqueueAsync(jobName, args, priority, delay);
            return jobId.ToString();
        }

        protected virtual async Task<Guid> EnqueueAsync(string jobName, object args, BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            var jobInfo = new BackgroundJobInfo
            {
                Id = GuidGenerator.Create(),
                JobName = jobName,
                JobArgs = Serializer.Serialize(args),
                Priority = priority,
                CreationTime = Clock.Now,
                NextTryTime = Clock.Now
            };

            if (delay.HasValue)
            {
                jobInfo.NextTryTime = Clock.Now.Add(delay.Value);
            }

            await Store.InsertAsync(jobInfo);

            return jobInfo.Id;
        }
    }
}
