

using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // Orders
    public partial class Order : BusinessEntity
    {
        public int OrderId { get; set; } // OrderID (Primary key)
        public DateTime? OrderDate { get; set; } // OrderDate
        public DateTime? ShipDate { get; set; } // ShipDate
        public int? CustomerId { get; set; } // CustomerId

        // Reverse navigation

        /// <summary>
        /// Child OrderItems where [OrderItems].[OrderId] point to this entity (FK_Orders_OrderItems)
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; } // OrderItems.FK_Orders_OrderItems

        // Foreign keys

        /// <summary>
        /// Parent Customer pointed by [Orders].([CustomerId]) (FK_Customer_Orders)
        /// </summary>
        public virtual Customer Customer { get; set; } // FK_Customer_Orders

        public Order()
        {
            OrderItems = new List<OrderItem>();
            InitializePartial();
        }

        partial void InitializePartial();

        public override object[] GetKeys()
        {
            return new object[] {this.OrderId};
        }
    }

}


