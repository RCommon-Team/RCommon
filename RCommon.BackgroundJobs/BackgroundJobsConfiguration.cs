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
    public class BackgroundJobsConfiguration : IServiceConfiguration
    {

        private IContainerAdapter _containerAdapter;
        private List<string> _jobTypes = new List<string>();

        public BackgroundJobsConfiguration()
        {

        }


        public void Configure(IContainerAdapter containerAdapter)
        {
            _containerAdapter = containerAdapter;
            _containerAdapter.AddTransient<IBackgroundJobExecuter, BackgroundJobExecuter>();
        }

        public IServiceConfiguration WithJobManager<TJobManager>()
            where TJobManager : IBackgroundJobManager
        {
            string type = typeof(TJobManager).AssemblyQualifiedName;
            _containerAdapter.AddTransient(typeof(IBackgroundJobManager), Type.GetType(type));
            return this;
        }

    }
}
