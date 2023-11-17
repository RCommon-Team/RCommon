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
    public class CreateLeaveAllocationDtoValidator : AbstractValidator<CreateLeaveAllocationDto>
    {
        private readonly IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> _leaveTypeRepository;

        public CreateLeaveAllocationDtoValidator(IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> leaveTypeRepository)
        {
            

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
