

using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // OrderItems
    public partial class OrderItem : BusinessEntity
    {
        public int OrderItemId { get; set; } // OrderItemID (Primary key)
        public decimal? Price { get; set; } // Price
        public int? Quantity { get; set; } // Quantity
        public string Store { get; set; } // Store (length: 255)
        public int? ProductId { get; set; } // ProductId
        public int? OrderId { get; set; } // OrderId

        // Foreign keys

        /// <summary>
        /// Parent Order pointed by [OrderItems].([OrderId]) (FK_Orders_OrderItems)
        /// </summary>
        public virtual Order Order { get; set; } // FK_Orders_OrderItems

        /// <summary>
        /// Parent Product pointed by [OrderItems].([ProductId]) (FK_OrderItems_Product)
        /// </summary>
        public virtual Product Product { get; set; } // FK_OrderItems_Product

        public OrderItem()
        {
            InitializePartial();
        }

        partial void InitializePartial();

        public override object[] GetKeys()
        {
            return new object[] { this.OrderItemId };
        }
    }

}

