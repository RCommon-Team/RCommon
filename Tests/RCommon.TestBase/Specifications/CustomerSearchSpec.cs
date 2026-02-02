using RCommon.TestBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.TestBase.Specifications
{
    public class CustomerSearchSpec : PagedSpecification<Customer>
    {
        public CustomerSearchSpec(string customerName,
            Expression<Func<Customer, object>> orderByExpression, bool orderByAscending, int pageIndex, int pageSize)
            : base(customer => customer.FirstName.StartsWith(customerName), orderByExpression, orderByAscending, pageIndex, pageSize)
        {
        }
    }
}
