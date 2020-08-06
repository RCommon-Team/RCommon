using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Samples.ConsoleApp.Shared.Dto
{
    public class OrderDto
    {
        public OrderDto()
        {

        }

        public int OrderId { get; set; } // OrderID (Primary key)
        public DateTime? OrderDate { get; set; } // OrderDate
        public DateTime? ShipDate { get; set; } // ShipDate
        public int? CustomerId { get; set; } // CustomerId
    }
}
