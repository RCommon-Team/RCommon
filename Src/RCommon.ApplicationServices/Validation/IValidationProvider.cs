using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Validation
{
    public interface IValidationProvider
    {
        Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults, CancellationToken cancellationToken = default)
        where T : class;
            
    }
}
