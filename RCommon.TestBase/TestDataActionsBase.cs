using Bogus;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.TestBase
{
    public abstract class TestDataActionsBase
    {

        public virtual Customer CreateCustomerStub(Action<Customer> customize)
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

        public virtual Customer CreateCustomerStub()
        {
            return CreateCustomerStub(x => { });
        }

        public virtual Order CreateOrderStub()
        {
            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            return order;
        }

        public OrderItem CreateOrderItemStub(Action<OrderItem> customize)
        {

            var orderItem = new Faker<OrderItem>()
                .RuleFor(x => x.Price, f => decimal.Parse(f.Commerce.Price(20, 599, 2)))
                .RuleFor(x => x.Quantity, f => f.Random.Int(1, 5))
                .RuleFor(x => x.Store, f => f.Company.CompanyName())
                .Generate();
            customize(orderItem);
            return orderItem;
        }

        public abstract Task<Customer> CreateCustomerAsync();

        public abstract Task<Customer> CreateCustomerAsync(Action<Customer> customize);
        public abstract Task<Order> CreateOrderAsync();
        public abstract Task<Order> CreateOrderForCustomerAsync(Customer customer);
        public abstract Task<Order> CreateOrderAsync(Action<Order> customize);

        public abstract Task<Product> CreateProductAsync();
        public abstract Task<Customer> GetCustomerAsync(Func<Customer, bool> spec);
        public abstract Task<Order> GetOrderAsync(Func<Order, bool> spec);
        public abstract Task<SalesPerson> GetSalesPersonAsync(Func<SalesPerson, bool> spec);
    }
}
