

using RCommon.BusinessEntities;
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
        public string StreetAddress1 { get; set; } // StreetAddress1 (length: 255)
        public string StreetAddress2 { get; set; } // StreetAddress2 (length: 255)
        public string City { get; set; } // City (length: 255)
        public string State { get; set; } // State (length: 255)
        public string ZipCode { get; set; } // ZipCode (length: 255)
        public string FirstName { get; set; } // FirstName (length: 255)
        public string LastName { get; set; } // LastName (length: 255)

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


