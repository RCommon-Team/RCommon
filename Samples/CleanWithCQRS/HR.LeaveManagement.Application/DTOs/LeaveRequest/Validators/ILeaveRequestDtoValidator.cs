using FluentValidation;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.DTOs.LeaveRequest.Validators
{
    public class ILeaveRequestDtoValidator : AbstractValidator<ILeaveRequestDto>
    {
        private readonly IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> _leaveTypeRepository;

        public ILeaveRequestDtoValidator(IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
            RuleFor(p => p.StartDate)
                .LessThan(p => p.EndDate).WithMessage("{PropertyName} must be before {ComparisonValue}");

            RuleFor(p => p.EndDate)
                .GreaterThan(p => p.StartDate).WithMessage("{PropertyName} must be after {ComparisonValue}");

            RuleFor(p => p.LeaveTypeId)
                .GreaterThan(0)
                .MustAsync(async (id, token) => {
                    var leaveTypeExists = await _leaveTypeRepository.GetCountAsync(x=>x.Id == id);
                    return (leaveTypeExists > 0 ? true : false);
                })
                .WithMessage("{PropertyName} does not exist.");
            
        }
    }
}
