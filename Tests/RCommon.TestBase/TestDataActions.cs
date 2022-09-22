using Bogus;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.TestBase
{
    public static class TestDataActions
    {

        public static Customer CreateCustomerStub(Action<Customer> customize)
        {
            var customer = new Faker<Customer>()
                .RuleFor(x => x.City, f => f.Address.City())
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.State, f => f.Address.State())
                .RuleFor(x => x.StreetAddress1, f => f.Address.StreetAddress())
                .RuleFor(x => x.StreetAddress2, f => f.Address.SecondaryAddress())
                .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
                .Generate();
            customize(customer);
            return customer;
        }

        public static Customer CreateCustomerStub()
        {
            return CreateCustomerStub(x => { });
        }
        public static SalesPerson CreateSalesPersonStub(Action<SalesPerson> customize)
        {
            var customer = new Faker<SalesPerson>()
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.SalesQuota, f => f.Random.Float())
                .RuleFor(x => x.SalesYtd, f => f.Random.Decimal())
                .Generate();
            customize(customer);
            return customer;
        }

        public static SalesPerson CreateSalesPersonStub()
        {
            return CreateSalesPersonStub(x => { });
        }

        public static Order CreateOrderStub()
        {
            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            return order;
        }

        public static Order CreateOrderStub(Action<Order> customize)
        {
            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            customize(order);
            return order;
        }

        public static OrderItem CreateOrderItemStub(Action<OrderItem> customize)
        {

            var orderItem = new Faker<OrderItem>()
                .RuleFor(x => x.Price, f => decimal.Parse(f.Commerce.Price(20, 599, 2)))
                .RuleFor(x => x.Quantity, f => f.Random.Int(1, 5))
                .RuleFor(x => x.Store, f => f.Company.CompanyName())
                .Generate();
            customize(orderItem);
            return orderItem;
        }

        
        public static Product CreateProductStub()
        {
            var product = new Faker<Product>()
                    .RuleFor(x => x.Description, f => f.Commerce.ProductMaterial())
                    .RuleFor(x => x.Name, f => f.Commerce.ProductName())
                    .Generate();
            return product;
        }
    }
}
