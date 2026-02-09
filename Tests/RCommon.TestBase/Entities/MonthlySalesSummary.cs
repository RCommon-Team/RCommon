

using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.TestBase.Entities
{
    // MonthlySalesSummary
    public partial class MonthlySalesSummary : BusinessEntity
    {
        public int Year { get; set; } // Year (Primary key)
        public int Month { get; set; } // Month (Primary key)
        public int SalesPersonId { get; set; } // SalesPersonId (Primary key)
        public decimal? Amount { get; set; } // Amount
        public string Currency { get; set; } = string.Empty; // Currency (length: 255)
        public string SalesPersonFirstName { get; set; } = string.Empty; // SalesPersonFirstName (length: 255)
        public string SalesPersonLastName { get; set; } = string.Empty; // SalesPersonLastName (length: 255)

        public MonthlySalesSummary()
        {
            InitializePartial();
        }

        partial void InitializePartial();

        public override object[] GetKeys()
        {
            return new object[] { this.SalesPersonId };
        }
    }

}


