using FluentValidation;
using RCommon.Persistence;
using RCommon.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.DTOs.LeaveAllocation.Validators
{
    public class ILeaveAllocationDtoValidator : AbstractValidator<ILeaveAllocationDto>
    {
        private readonly IReadOnlyRepository<Domain.LeaveType> _leaveTypeRepository;

        public ILeaveAllocationDtoValidator(IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> leaveTypeRepository)
        {
            
            RuleFor(p => p.NumberOfDays)
                .GreaterThan(0).WithMessage("{PropertyName} must greater than {ComparisonValue}");

            RuleFor(p => p.Period)
                .GreaterThanOrEqualTo(DateTime.Now.Year).WithMessage("{PropertyName} must be after {ComparisonValue}");

            RuleFor(p => p.LeaveTypeId)
                .GreaterThan(0)
                .MustAsync(async (id, token) => 
                {
                    var leaveTypeExists = await _leaveTypeRepository.GetCountAsync(x=>x.Id == id);
                    return (leaveTypeExists > 0 ? true : false);
                })
                .WithMessage("{PropertyName} does not exist.");
            this._leaveTypeRepository = leaveTypeRepository;
        }
    }
}
