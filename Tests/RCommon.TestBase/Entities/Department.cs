

using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // Departments
    public partial class Department : BusinessEntity<int>
    {
        public string Name { get; set; } // Name (length: 255)

        // Reverse navigation

        /// <summary>
        /// Child SalesPersons where [SalesPerson].[DepartmentId] point to this entity (FK74214A90E25FF6)
        /// </summary>
        public virtual ICollection<SalesPerson> SalesPersons { get; set; } // SalesPerson.FK74214A90E25FF6

        public Department()
        {
            SalesPersons = new List<SalesPerson>();
            InitializePartial();
        }

        partial void InitializePartial();
    }

}

