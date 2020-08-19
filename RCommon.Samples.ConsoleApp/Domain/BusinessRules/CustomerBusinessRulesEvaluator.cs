using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RCommon.Samples.ConsoleApp.Domain.BusinessRules
{
    public class CustomerBusinessRulesEvaluator : BusinessRulesEvaluatorBase<Customer>
    {
        public CustomerBusinessRulesEvaluator()
        {
            var rule = new BusinessRule<Customer>(
                new Specification<Customer>(x => x.ZipCode != "30062"),
                    x => this.SomeImportantBusinessAction(x)
                );
            this.AddRule("ZipCodeRule", rule);
        }

        private void SomeImportantBusinessAction(Customer customer)
        {
            Debug.WriteLine("We are doing something important related to the business rule for " + customer.FirstName + " " + customer.LastName);
        }
    }
}
