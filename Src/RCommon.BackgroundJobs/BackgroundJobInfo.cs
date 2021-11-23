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

namespace RCommon.BackgroundJobs
{
    /// <summary>
    /// Represents a background job info that is used to persist jobs.
    /// </summary>
    public class BackgroundJobInfo
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the job.
        /// </summary>
        public virtual string JobName { get; set; }

        /// <summary>
        /// Job arguments as serialized to string.
        /// </summary>
        public virtual string JobArgs { get; set; }

        /// <summary>
        /// Try count of this job.
        /// A job is re-tried if it fails.
        /// </summary>
        public virtual short TryCount { get; set; }

        /// <summary>
        /// Creation time of this job.
        /// </summary>
        public virtual DateTime CreationTime { get; set; }

        /// <summary>
        /// Next try time of this job.
        /// </summary>
        public virtual DateTime NextTryTime { get; set; }

        /// <summary>
        /// Last try time of this job.
        /// </summary>
        public virtual DateTime? LastTryTime { get; set; }

        /// <summary>
        /// This is true if this job is continuously failed and will not be executed again.
        /// </summary>
        public virtual bool IsAbandoned { get; set; }

        /// <summary>
        /// Priority of this job.
        /// </summary>
        public virtual BackgroundJobPriority Priority { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundJobInfo"/> class.
        /// </summary>
        public BackgroundJobInfo()
        {
            Priority = BackgroundJobPriority.Normal;
        }
    }
}
