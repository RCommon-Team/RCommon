﻿using Bogus;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public class EFTestDataActions
    {
        readonly EFTestData _generator;

        public EFTestDataActions(EFTestData generator)
        {
            _generator = generator;
            
        }


        public Customer CreateCustomerStub(Action<Customer> customize)
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

        public Customer CreateCustomerStub()
        {
            return CreateCustomerStub(x => { });
        }

        public async Task<Customer> CreateCustomerAsync()
        {
            return await CreateCustomerAsync(x => { });
        }

        public async Task<Customer> CreateCustomerAsync(Action<Customer> customize)
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

        public async Task<Order> CreateOrder()
        {
            return await CreateOrder(x => { });
        }

        public async Task<Order> CreateOrderForCustomer(Customer customer)
        {
            var order = await CreateOrder(x => x.Customer = customer);
            return order;
        }

        public async Task<Order> CreateOrder(Action<Order> customize)
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

        public Order CreateOrderStub()
        {
            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            return order;
        }

        public OrderItem CreateOrderItem(Action<OrderItem> customize)
        {
            
            var orderItem = new Faker<OrderItem>()
                .RuleFor(x => x.Price, f => decimal.Parse(f.Commerce.Price(20, 599, 2)))
                .RuleFor(x => x.Quantity, f => f.Random.Int(1, 5))
                .RuleFor(x => x.Store, f => f.Company.CompanyName())
                .Generate();
            customize(orderItem);
            return orderItem;
        }

        public Product CreateProduct()
        {
            var product = new Faker<Product>()
                    .RuleFor(x => x.Description, f => f.Commerce.ProductMaterial())
                    .RuleFor(x => x.Name, f => f.Commerce.ProductName())
                    .Generate();
            _generator.Context.Set<Product>().AddAsync(product);
            _generator.Context.SaveChangesAsync();
            return product;
        }

        public Customer GetCustomerById(int customerId)
        {
            
            var customer = _generator.Context.Set<Customer>()
                .Where(x => x.Id == customerId)
                .FirstOrDefault();
            if (customer != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Customer>().Remove(customer));
            return customer;
        }

        public Customer GetFirstCustomer(Func<Customer, bool> spec)
        {

            var customer = _generator.Context.Set<Customer>()
                .Where(spec)
                .FirstOrDefault();
            if (customer != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Customer>().Remove(customer));
            return customer;
        }

        public Order GetOrderById(int orderId)
        {
            var order = _generator.Context.Set<Order>()
                .Where(x => x.OrderId == orderId)
                .FirstOrDefault();
            if (order != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Order>().Remove(order));
            return order;
        }

        public SalesPerson GetSalesPersonById(int id)
        {
            var salesPerson = _generator.Context.Set<SalesPerson>()
                .Where(x => x.Id == id)
                .FirstOrDefault();
            if (salesPerson != null)
                _generator.EntityDeleteActions.Add(x => x.Set<SalesPerson>().Remove(salesPerson));
            return salesPerson;
        }
    }
}
