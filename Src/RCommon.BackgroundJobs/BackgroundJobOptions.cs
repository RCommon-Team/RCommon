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
using System.Collections.Immutable;
using RCommon.Extensions;

namespace RCommon.BackgroundJobs
{
    public class BackgroundJobOptions
    {
        private readonly Dictionary<Type, BackgroundJobSettings> _jobConfigurationsByArgsType;
        private readonly Dictionary<string, BackgroundJobSettings> _jobConfigurationsByName;
        
        /// <summary>
        /// Default: true.
        /// </summary>
        public bool IsJobExecutionEnabled { get; set; } = true;

        public BackgroundJobOptions()
        {
            _jobConfigurationsByArgsType = new Dictionary<Type, BackgroundJobSettings>();
            _jobConfigurationsByName = new Dictionary<string, BackgroundJobSettings>();
        }

        public BackgroundJobSettings GetJob<TArgs>()
        {
            return GetJob(typeof(TArgs));
        }

        public BackgroundJobSettings GetJob(Type argsType)
        {
            var jobConfiguration = _jobConfigurationsByArgsType.GetOrDefault(argsType);

            if (jobConfiguration == null)
            {
                throw new GeneralException("Undefined background job for the job args type: " + argsType.AssemblyQualifiedName);
            }

            return jobConfiguration;
        }

        public BackgroundJobSettings GetJob(string name)
        {
            var jobConfiguration = _jobConfigurationsByName.GetOrDefault(name);

            if (jobConfiguration == null)
            {
                throw new GeneralException("Undefined background job for the job name: " + name);
            }

            return jobConfiguration;
        }

        public IReadOnlyList<BackgroundJobSettings> GetJobs()
        {
            return _jobConfigurationsByArgsType.Values.ToImmutableList();
        }

        public void AddJob<TJob>()
        {
            AddJob(typeof(TJob));
        }

        public void AddJob(Type jobType)
        {
            AddJob(new BackgroundJobSettings(jobType));
        }

        public void AddJob(BackgroundJobSettings jobConfiguration)
        {
            _jobConfigurationsByArgsType[jobConfiguration.ArgsType] = jobConfiguration;
            _jobConfigurationsByName[jobConfiguration.JobName] = jobConfiguration;
        }
    }
}
