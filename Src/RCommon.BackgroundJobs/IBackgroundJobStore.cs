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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.BackgroundJobs
{
    /// <summary>
    /// Defines interface to store/get background jobs.
    /// </summary>
    public interface IBackgroundJobStore
    {
        /// <summary>
        /// Gets a BackgroundJobInfo based on the given jobId.
        /// </summary>
        /// <param name="jobId">The Job Unique Identifier.</param>
        /// <returns>The BackgroundJobInfo object.</returns>
        Task<BackgroundJobInfo> FindAsync(Guid jobId);

        /// <summary>
        /// Inserts a background job.
        /// </summary>
        /// <param name="jobInfo">Job information.</param>
        Task InsertAsync(BackgroundJobInfo jobInfo);

        /// <summary>
        /// Gets waiting jobs. It should get jobs based on these:
        /// Conditions: !IsAbandoned And NextTryTime &lt;= Clock.Now.
        /// Order by: Priority DESC, TryCount ASC, NextTryTime ASC.
        /// Maximum result: <paramref name="maxResultCount"/>.
        /// </summary>
        /// <param name="maxResultCount">Maximum result count.</param>
        Task<List<BackgroundJobInfo>> GetWaitingJobsAsync(int maxResultCount);

        /// <summary>
        /// Deletes a job.
        /// </summary>
        /// <param name="jobId">The Job Unique Identifier.</param>
        Task DeleteAsync(Guid jobId);

        /// <summary>
        /// Updates a job.
        /// </summary>
        /// <param name="jobInfo">Job information.</param>
        Task UpdateAsync(BackgroundJobInfo jobInfo);
    }
}
