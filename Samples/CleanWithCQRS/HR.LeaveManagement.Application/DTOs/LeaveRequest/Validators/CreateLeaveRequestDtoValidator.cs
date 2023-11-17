using FluentValidation;
using RCommon.Persistence;
using RCommon.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.DTOs.LeaveRequest.Validators
{
    public class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
    {
        private readonly IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> _leaveTypeRepository;

        public CreateLeaveRequestDtoValidator(IReadOnlyRepository<HR.LeaveManagement.Domain.LeaveType> leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
            Include(new ILeaveRequestDtoValidator(_leaveTypeRepository));
        }
    }
}
