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

using RCommon.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RCommon.BackgroundJobs
{
    public class InMemoryBackgroundJobStore : IBackgroundJobStore
    {
        private readonly ConcurrentDictionary<Guid, BackgroundJobInfo> _jobs;

        protected ISystemTime Clock { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryBackgroundJobStore"/> class.
        /// </summary>
        public InMemoryBackgroundJobStore(ISystemTime clock)
        {
            Clock = clock;
            _jobs = new ConcurrentDictionary<Guid, BackgroundJobInfo>();
        }

        public virtual Task<BackgroundJobInfo> FindAsync(Guid jobId)
        {
            return Task.FromResult(_jobs.GetOrDefault(jobId));
        }

        public virtual Task InsertAsync(BackgroundJobInfo jobInfo)
        {
            _jobs[jobInfo.Id] = jobInfo;

            return Task.FromResult(0);
        }

        public virtual Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount)
        {
            var waitingJobs = _jobs.Values
                .Where(t => !t.IsAbandoned && t.NextTryTime <= Clock.Now)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.TryCount)
                .ThenBy(t => t.NextTryTime)
                .Take(maxResultCount)
                .ToList();

            return Task.FromResult(waitingJobs);
        }


        public virtual Task DeleteAsync(Guid jobId)
        {
            _jobs.TryRemove(jobId, out _);

            return Task.FromResult(0);
        }
        
        public virtual Task UpdateAsync(BackgroundJobInfo jobInfo)
        {
            if (jobInfo.IsAbandoned)
            {
                return DeleteAsync(jobInfo.Id);
            }

            return Task.FromResult(0);
        }
    }
}
