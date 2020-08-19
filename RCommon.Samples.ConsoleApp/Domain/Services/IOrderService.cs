using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.Domain.Services
{
    public interface IOrderService
    {
        Task CreateOrderAsync(Order order);
    }
}