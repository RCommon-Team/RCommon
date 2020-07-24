using Bogus;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RCommon.ObjectAccess.EFCore.Tests
{
    public class EFTestDataActions
    {
        readonly EFTestData _generator;

        public EFTestDataActions(EFTestData generator)
        {
            _generator = generator;
            
        }


        public Customer CreateCustomerStub()
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
            return customer;
        }

        public Customer CreateCustomer()
        {
            return CreateCustomer(x => { });
        }

        public Customer CreateCustomer(Action<Customer> customize)
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
            _generator.Context.Set<Customer>().Add(customer);
            _generator.Context.SaveChanges();
            return customer;
        }

        public Order CreateOrder()
        {
            return CreateOrder(x => { });
        }

        public Order CreateOrderForCustomer(Customer customer)
        {
            var order = CreateOrder(x => x.Customer = customer);
            customer.Orders.Add(order);
            return order;
        }

        public Order CreateOrder(Action<Order> customize)
        {
            

            var order = new Faker<Order>()
                .RuleFor(x => x.OrderDate, f => f.Date.Past(2))
                .RuleFor(x => x.ShipDate, f => f.Date.Past(2))
                .Generate();
            customize(order);

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
            _generator.Context.Set<Product>().Add(product);
            return product;
        }

        public Customer GetCustomerById(int customerId)
        {
            
            var customer = _generator.Context.Set<Customer>()
                .Where(x => x.CustomerId == customerId)
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
