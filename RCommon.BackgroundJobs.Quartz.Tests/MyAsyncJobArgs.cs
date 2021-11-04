namespace RCommon.BackgroundJobs.Quartz.Tests
{
    public class MyAsyncJobArgs
    {
        public string Value { get; set; }

        public MyAsyncJobArgs()
        {

        }

        public MyAsyncJobArgs(string value)
        {
            Value = value;
        }
    }
}