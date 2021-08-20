using Bogus;
using RCommon.Extensions;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public class EFTestDataActions : TestDataActionsBase
    {
        readonly EFTestData _generator;

        public EFTestDataActions(EFTestData generator)
        {
            _generator = generator;

        }

        public override async Task<Customer> CreateCustomerAsync()
        {
            return await CreateCustomerAsync(x => { });
        }

        public override async Task<Customer> CreateCustomerAsync(Action<Customer> customize)
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
            await _generator.Context.Set<Customer>().AddAsync(customer);
            await _generator.Context.SaveChangesAsync();
            return customer;
        }

        public override async Task<Order> CreateOrderAsync()
        {
            return await CreateOrderAsync(x => { });
        }

        public override async Task<Order> CreateOrderForCustomerAsync(Customer customer)
        {
            var order = await CreateOrderAsync(x => x.Customer = customer);
            return order;
        }

        public override async Task<Order> CreateOrderAsync(Action<Order> customize)
        {
            

            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            customize(order);
            await _generator.Context.Set<Order>().AddAsync(order);
            await _generator.Context.SaveChangesAsync();

            return order;
        }

        public override async Task<Product> CreateProductAsync()
        {
            var product = new Faker<Product>()
                    .RuleFor(x => x.Description, f => f.Commerce.ProductMaterial())
                    .RuleFor(x => x.Name, f => f.Commerce.ProductName())
                    .Generate();
            await _generator.Context.Set<Product>().AddAsync(product);
            await _generator.Context.SaveChangesAsync();
            return product;
        }

        public async override Task<Customer> GetCustomerAsync(Func<Customer, bool> spec)
        {

            var customer = _generator.Context.Set<Customer>()
                .Where(spec)
                .FirstOrDefault();
            if (customer != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Customer>().Remove(customer));
            return await Task.FromResult(customer);
        }

        public async override Task<Order> GetOrderAsync(Func<Order, bool> spec)
        {
            var order = _generator.Context.Set<Order>()
                .Where(spec)
                .FirstOrDefault();
            if (order != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Order>().Remove(order));
            return await Task.FromResult(order);
        }

        public async override Task<SalesPerson> GetSalesPersonAsync(Func<SalesPerson, bool> spec)
        {
            var salesPerson = _generator.Context.Set<SalesPerson>()
                .Where(spec)
                .FirstOrDefault();
            if (salesPerson != null)
                _generator.EntityDeleteActions.Add(x => x.Set<SalesPerson>().Remove(salesPerson));
            return await Task.FromResult(salesPerson);
        }
    }
}
