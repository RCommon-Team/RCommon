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
using Quartz;

namespace RCommon.BackgroundJobs.Quartz
{
    public static class QuartzBackgroundJobManageExtensions
    {
        public static async Task<string> EnqueueAsync<TArgs>(this IBackgroundJobManager backgroundJobManager,
            TArgs args, int retryCount, int retryIntervalMillisecond,
            BackgroundJobPriority priority = BackgroundJobPriority.Normal, TimeSpan? delay = null)
        {
            if (backgroundJobManager is QuartzBackgroundJobManager quartzBackgroundJobManager)
            {
                return await quartzBackgroundJobManager.ReEnqueueAsync(args, retryCount, retryIntervalMillisecond,
                    priority, delay);
            }

            return null;
        }
    }
}
