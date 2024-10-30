using RCommon.ApplicationServices.Validation;

namespace Examples.Validation.FluentValidation
{
    public interface ITestApplicationService
    {
        Task<ValidationOutcome> ExecuteTestMethod(TestDto dto);
    }
}
