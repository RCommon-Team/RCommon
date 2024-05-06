using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Validation
{
    public interface IValidationService
    {
        Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults = false, CancellationToken cancellationToken = default) where T : class;
    }
}