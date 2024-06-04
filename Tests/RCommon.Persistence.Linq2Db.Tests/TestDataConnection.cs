using LinqToDB;
using LinqToDB.Configuration;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db.Tests
{
    public class TestDataConnection : RCommonDataConnection
    {
        public TestDataConnection(DataOptions linq2DbOptions) 
            : base(linq2DbOptions)
        {
        }

        

        public ITable<Customer> Customers => this.GetTable<Customer>();
        public ITable<Department> Departments => this.GetTable<Department>();
        public ITable<MonthlySalesSummary> MonthlySalesSummaries => this.GetTable<MonthlySalesSummary>();
        public ITable<Order> Orders => this.GetTable<Order>();
        public ITable<OrderItem> OrderItems => this.GetTable<OrderItem>();
        public ITable<Product> Products => this.GetTable<Product>();
        public ITable<SalesPerson> SalesPersons => this.GetTable<SalesPerson>();
        public ITable<SalesTerritory> SalesTerritories => this.GetTable<SalesTerritory>();
    }
}
