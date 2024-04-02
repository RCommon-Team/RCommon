using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveRequest.Validators;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands;
using HR.LeaveManagement.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence;
using RCommon.Persistence.Crud;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Commands
{
    public class UpdateLeaveRequestCommandHandler : IRequestHandler<UpdateLeaveRequestCommand, Unit>
    {
        private readonly IGraphRepository<LeaveRequest> _leaveRequestRepository;
        private readonly IReadOnlyRepository<LeaveType> _leaveTypeRepository;
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;
        private readonly IMapper _mapper;

        public UpdateLeaveRequestCommandHandler(
            IGraphRepository<LeaveRequest> leaveRequestRepository,
            IReadOnlyRepository<LeaveType> leaveTypeRepository,
            IGraphRepository<LeaveAllocation> leaveAllocationRepository,
             IMapper mapper)
        {
            this._leaveRequestRepository = leaveRequestRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _leaveAllocationRepository = leaveAllocationRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveRequestRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            _mapper = mapper;
        }

        public async Task<Unit> Handle(UpdateLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await _leaveRequestRepository.FindAsync(request.Id);

            if(leaveRequest is null)
                throw new NotFoundException(nameof(leaveRequest), request.Id);

            if (request.LeaveRequestDto != null)
            {
                var validator = new UpdateLeaveRequestDtoValidator(_leaveTypeRepository);
                var validationResult = await validator.ValidateAsync(request.LeaveRequestDto);
                if (validationResult.IsValid == false)
                    throw new ValidationException(validationResult);

                _mapper.Map(request.LeaveRequestDto, leaveRequest);

                await _leaveRequestRepository.UpdateAsync(leaveRequest);
            }
            else if(request.ChangeLeaveRequestApprovalDto != null)
            {
                leaveRequest.Approved = request.ChangeLeaveRequestApprovalDto.Approved;
                await _leaveRequestRepository.UpdateAsync(leaveRequest);
                //await _leaveRequestRepository.ChangeApprovalStatus(leaveRequest, request.ChangeLeaveRequestApprovalDto.Approved);
                if (request.ChangeLeaveRequestApprovalDto.Approved)
                {
                    var allocation = _leaveAllocationRepository.FirstOrDefault(q => q.EmployeeId == leaveRequest.RequestingEmployeeId
                                        && q.LeaveTypeId == leaveRequest.LeaveTypeId);
                    int daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;

                    allocation.NumberOfDays -= daysRequested;

                    await _leaveAllocationRepository.UpdateAsync(allocation);
                }
            }

            return Unit.Value;
        }
    }
}
