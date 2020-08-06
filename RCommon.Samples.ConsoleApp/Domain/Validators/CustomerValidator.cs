using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Validation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RCommon.Samples.ConsoleApp.Domain.Validators
{
    public class CustomerValidator : EntityValidatorBase<Customer>
    {
        public CustomerValidator()
        {
            this.AddValidation("", new ValidationRule<Customer>(
                new Specification<Customer>(x => x.ZipCode != "30062"), "We don't like people from that zip code!", "ZipCode"));
        }
    }
}
