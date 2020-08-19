using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Samples.ConsoleApp.Domain.Repositories
{
    public interface IOrderRepository : IEncapsulatedRepository<Order>
    {
    }
}
