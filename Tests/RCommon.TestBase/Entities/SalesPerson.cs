

using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // SalesPerson
    public partial class SalesPerson : BusinessEntity<int>
    {
        public string FirstName { get; set; } // FirstName (length: 255)
        public string LastName { get; set; } // LastName (length: 255)
        public float? SalesQuota { get; set; } // SalesQuota
        public decimal? SalesYtd { get; set; } // SalesYTD
        public int? DepartmentId { get; set; } // DepartmentId
        public int? TerritoryId { get; set; } // TerritoryId

        // Foreign keys

        /// <summary>
        /// Parent Department pointed by [SalesPerson].([DepartmentId]) (FK74214A90E25FF6)
        /// </summary>
        public virtual Department Department { get; set; } // FK74214A90E25FF6

        /// <summary>
        /// Parent SalesTerritory pointed by [SalesPerson].([TerritoryId]) (FK74214A90B23DB0A3)
        /// </summary>
        public virtual SalesTerritory SalesTerritory { get; set; } // FK74214A90B23DB0A3

        public SalesPerson()
        {
            InitializePartial();
        }

        partial void InitializePartial();

    }

}


