
namespace Examples.Caching.MemoryCaching
{
    public interface ITestApplicationService
    {
        Task GetCache();
        Task SetCache();
    }
}