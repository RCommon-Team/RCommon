

using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // SalesTerritory
    public partial class SalesTerritory : BusinessEntity<int>
    {
        public string Name { get; set; } = string.Empty; // Name (length: 255)
        public string Description { get; set; } = string.Empty; // Description (length: 255)

        // Reverse navigation

        /// <summary>
        /// Child SalesPersons where [SalesPerson].[TerritoryId] point to this entity (FK74214A90B23DB0A3)
        /// </summary>
        public virtual ICollection<SalesPerson> SalesPersons { get; set; } // SalesPerson.FK74214A90B23DB0A3

        public SalesTerritory()
        {
            SalesPersons = new List<SalesPerson>();
            InitializePartial();
        }

        partial void InitializePartial();

    }

}


