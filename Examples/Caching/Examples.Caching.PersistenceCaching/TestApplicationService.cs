using RCommon.Persistence.Crud;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon.Persistence.Caching.Crud;

namespace Examples.Caching.PersistenceCaching
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly ICachingGraphRepository<Customer> _customerRepository;

        public TestApplicationService(ICachingGraphRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
            _customerRepository.DataStoreName = "TestDbContext";
        }

        public async Task<ICollection<Customer>> GetCustomers(object cacheKey)
        {
            return await _customerRepository.FindAsync(cacheKey, x => x.LastName == "Potter");
        }


    }
}
