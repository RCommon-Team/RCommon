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
