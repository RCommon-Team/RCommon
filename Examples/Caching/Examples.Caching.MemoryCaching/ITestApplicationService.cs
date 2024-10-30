
namespace Examples.Caching.MemoryCaching
{
    public interface ITestApplicationService
    {
        TestDto GetDistributedMemoryCache(string key);
        TestDto GetMemoryCache(string key);
        void SetDistributedMemoryCache(string key, Type type, object data);
        void SetMemoryCache(string key, TestDto data);
    }
}