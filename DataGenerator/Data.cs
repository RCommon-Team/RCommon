using Bogus;
using RCommon.Extensions;
using RCommon.ObjectAccess.EFCore.Tests;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DataGenerator
{
    public class EFTestDataActions
    {
        readonly EFTestData _generator;

        public EFTestDataActions(EFTestData generator)
        {
            _generator = generator;

        }


       

        public ApplicationUser CreateCustomer(Action<ApplicationUser> customize)
        {

            var customer = new Faker<ApplicationUser>()
                .RuleFor(x => x.City, f => f.Address.City())
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.State, f => f.Address.State())
                .RuleFor(x => x.StreetAddress1, f => f.Address.StreetAddress())
                .RuleFor(x => x.StreetAddress2, f => f.Address.SecondaryAddress())
                .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
                .Generate();
            customize(customer);
            _generator.Context.Set<ApplicationUser>().Add(customer);
            _generator.Context.SaveChanges();
            return customer;
        }


    }
}
