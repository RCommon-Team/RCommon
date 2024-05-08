using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.ApplicationServices.Queries;
using RCommon.ApplicationServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Validation.FluentValidation
{
    public class TestApplicationService : ITestApplicationService
    {
        private readonly IValidationService _validationService;

        public TestApplicationService(IValidationService validationService)
        {
            _validationService = validationService;
        }

        public async Task<ValidationOutcome> ExecuteTestMethod(TestDto dto)
        {
            return await _validationService.ValidateAsync(dto);
        }
    }
}
