namespace RCommon.BackgroundJobs
{
    public interface IBackgroundJobNameProvider
    {
        string Name { get; }
    }
}