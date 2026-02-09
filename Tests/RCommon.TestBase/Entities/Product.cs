

using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // Products
    public partial class Product : BusinessEntity
    {
        public int ProductId { get; set; } // ProductID (Primary key)
        public string Name { get; set; } = string.Empty; // Name (length: 255)
        public string Description { get; set; } = string.Empty; // Description (length: 255)

        // Reverse navigation

        /// <summary>
        /// Child OrderItems where [OrderItems].[ProductId] point to this entity (FK_OrderItems_Product)
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; } // OrderItems.FK_OrderItems_Product

        public Product()
        {
            OrderItems = new List<OrderItem>();
            InitializePartial();
        }

        partial void InitializePartial();

        public override object[] GetKeys()
        {
            return new object[] { this.ProductId };
        }
    }

}


