using RCommon.TestBase.Entities;

namespace Examples.Caching.PersistenceCaching
{
    public interface ITestApplicationService
    {
        Task<ICollection<Customer>> GetCustomers(object cacheKey);
    }
}