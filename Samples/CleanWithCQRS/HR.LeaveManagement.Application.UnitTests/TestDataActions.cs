using Bogus;
using HR.LeaveManagement.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.UnitTests
{
    public static class TestDataActions
    {
        public static LeaveType CreateLeaveTypeStub(Action<LeaveType> customize)
        {
            var customer = new Faker<LeaveType>()
                .RuleFor(x => x.DefaultDays, f => f.Random.Int())
                .RuleFor(x => x.CreatedBy, f => f.Name.FirstName())
                .RuleFor(x => x.DateCreated, f => f.Date.Recent())
                .RuleFor(x => x.DateLastModified, f => f.Date.Recent())
                .RuleFor(x => x.Id, f => f.Random.Int())
                .RuleFor(x => x.LastModifiedBy, f => f.Name.LastName())
                .RuleFor(x => x.Name, f => f.Name.FullName())
                .Generate();
            customize(customer);
            return customer;
        }

        public static LeaveType CreateLeaveTypeStub()
        {
            return CreateLeaveTypeStub(x => { });
        }
    }
}
