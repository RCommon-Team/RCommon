
namespace Examples.Caching.RedisCaching
{
    public interface ITestApplicationService
    {
        TestDto GetDistributedMemoryCache(string key);
        void SetDistributedMemoryCache(string key, Type type, object data);
    }
}