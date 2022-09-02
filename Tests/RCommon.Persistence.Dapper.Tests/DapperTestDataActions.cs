﻿using Bogus;
using DapperExtensions;
using DapperExtensions.Mapper;
using DapperExtensions.Sql;
using RCommon.TestBase;
using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DapperSqlMapperExtensions = Dapper.Contrib.Extensions;

namespace RCommon.Persistence.Dapper.Tests
{
    public class DapperTestDataActions : TestDataActionsBase
    {

        readonly DapperTestData _generator;

        public DapperTestDataActions(DapperTestData generator)
        {
            _generator = generator;

        }


        protected virtual AsyncDatabase GetAsyncDatabase(DbConnection connection, SqlDialectBase sqlDialect)
        {
            var config = new DapperExtensionsConfiguration(typeof(PluralizedAutoClassMapper<>), new List<Assembly>(), sqlDialect);
            var sqlGenerator = new SqlGeneratorImpl(config);
            var db = new AsyncDatabase(connection, sqlGenerator);
            return db;
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

            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                
                await db.Insert<Customer>(customer, 30);
            }
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

            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                await db.Insert<Order>(order, 30);
            }

            return order;
        }

        public override async Task<Product> CreateProductAsync()
        {
            var product = new Faker<Product>()
                    .RuleFor(x => x.Description, f => f.Commerce.ProductMaterial())
                    .RuleFor(x => x.Name, f => f.Commerce.ProductName())
                    .Generate();

            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                await db.Insert<Product>(product, 30);
            }

            return product;
        }

        public override async Task<Customer> GetCustomerAsync(Func<Customer, bool> spec)
        {

            Customer customer;

            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                
                var data = await db.Connection.GetPageAsync<Customer>(spec, null, 1, 1, null, null, false);
                customer = data.First();
            }

            /*if (customer != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Customer>().Remove(customer));*/
            return customer;
        }

        public async override Task<Order> GetOrderAsync(Func<Order, bool> spec)
        {
            Order order;
            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                var data = await db.Connection.GetPageAsync<Order>(spec, null, 1, 1, null, null, false);
                order = data.First();
            }
            /*if (order != null)
                _generator.EntityDeleteActions.Add(x => x.Set<Order>().Remove(order));*/
            return order;
        }

        public async override Task<SalesPerson> GetSalesPersonAsync(Func<SalesPerson, bool> spec)
        {
            SalesPerson salesPerson;
            using (var db = this.GetAsyncDatabase(this._generator.Context.GetDbConnection(), new SqlServerDialect()))
            {
                var data = await db.Connection.GetPageAsync<SalesPerson>(spec, null, 1, 1, null, null, false);
                salesPerson = data.First();
            }
            /*if (salesPerson != null)
                _generator.EntityDeleteActions.Add(x => x.Set<SalesPerson>().Remove(salesPerson));*/
            return salesPerson;
        }
    }
}
