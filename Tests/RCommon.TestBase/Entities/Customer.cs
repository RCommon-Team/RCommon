

using RCommon.Entities;
using RCommon.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // Customers
    public partial class Customer : BusinessEntity<int>
    {
        public string StreetAddress1 { get; set; } = string.Empty; // StreetAddress1 (length: 255)
        public string StreetAddress2 { get; set; } = string.Empty; // StreetAddress2 (length: 255)
        public string City { get; set; } = string.Empty; // City (length: 255)
        public string State { get; set; } = string.Empty; // State (length: 255)
        public string ZipCode { get; set; } = string.Empty; // ZipCode (length: 255)
        public string FirstName { get; set; } = string.Empty; // FirstName (length: 255)
        public string LastName { get; set; } = string.Empty; // LastName (length: 255)

        // Reverse navigation

        /// <summary>
        /// Child Orders where [Orders].[CustomerId] point to this entity (FK_Customer_Orders)
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } // Orders.FK_Customer_Orders

        public Customer()
        {
            Orders = new List<Order>();
            InitializePartial();
        }

        partial void InitializePartial();

    }

}


