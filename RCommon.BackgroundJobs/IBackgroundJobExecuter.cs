using System.Threading.Tasks;

namespace RCommon.BackgroundJobs
{
    public interface IBackgroundJobExecuter
    {
        Task ExecuteAsync(JobExecutionContext context);
    }
}