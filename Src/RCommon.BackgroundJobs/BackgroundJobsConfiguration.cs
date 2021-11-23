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

using Microsoft.Extensions.DependencyInjection;
using RCommon.Configuration;
using RCommon.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BackgroundJobs
{
    public class BackgroundJobsConfiguration : RCommonConfiguration
    {
        private List<string> _jobTypes = new List<string>();

        public BackgroundJobsConfiguration(IContainerAdapter containerAdapter):base(containerAdapter)
        {

        }


        public override void Configure()
        {
            this.ContainerAdapter.AddTransient<IBackgroundJobExecuter, BackgroundJobExecuter>();
        }

        public IServiceConfiguration WithJobManager<TJobManager>()
            where TJobManager : IBackgroundJobManager
        {
            string type = typeof(TJobManager).AssemblyQualifiedName;
            this.ContainerAdapter.AddTransient(typeof(IBackgroundJobManager), Type.GetType(type));
            return this;
        }

    }
}
