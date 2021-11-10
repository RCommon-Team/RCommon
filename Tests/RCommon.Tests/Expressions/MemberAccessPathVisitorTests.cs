
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using NUnit.Framework;
using RCommon.Expressions;

namespace NCommon.Expressions.Tests
{
    /// <summary>
    /// Test the <see cref="MemberAccessPathVisitor"/> class.
    /// </summary>
    [TestFixture]
    public class MemberAccessPathVisitorTests
    {
        #region Test Classes
        public class SalesPerson
        {
            public Customer PrimaryCustomer { get; set; }

            public object MethodAccess() { return null; }
        }

        public class Customer { public IList<Order> Orders; }

        public class Order { }
        #endregion

        [Test]
        public void Visit_Customer_Orders_Should_Return_Orders_As_Path()
        {
            Expression<Func<Customer, object>> expression = x => x.Orders;
            var visitor = new MemberAccessPathVisitor();
            visitor.Visit(expression);
            Assert.AreEqual("Orders", visitor.Path);
        }

        [Test]
        public void Visit_SalesPerson_Customer_Orders_Should_Return_Customer_Orders_As_Path()
        {
            Expression<Func<SalesPerson, object>> expression = x => x.PrimaryCustomer.Orders;
            var visitor = new MemberAccessPathVisitor();
            visitor.Visit(expression);
            Assert.AreEqual("PrimaryCustomer.Orders", visitor.Path);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void Visit_Throws_NotSupportedException_When_Expression_Contains_Method_Call()
        {
            Expression<Func<SalesPerson, object>> expression = x => x.MethodAccess();
            var visitor = new MemberAccessPathVisitor();
            visitor.Visit(expression);
        }
    }


}
