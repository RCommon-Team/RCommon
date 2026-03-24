using HR.LeaveManagement.Application.DTOs;
using HR.LeaveManagement.Application.DTOs.LeaveType;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
using HR.LeaveManagement.Application.Mappings;
using HR.LeaveManagement.Domain;
using RCommon.Mediator.Subscribers;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Queries
{
    public class GetLeaveTypeDetailRequestHandler : IAppRequestHandler<GetLeaveTypeDetailRequest, LeaveTypeDto>
    {
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;

        public GetLeaveTypeDetailRequestHandler(IGraphRepository<LeaveType> leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task<LeaveTypeDto> HandleAsync(GetLeaveTypeDetailRequest request, CancellationToken cancellationToken)
        {
            var leaveType = await _leaveTypeRepository.FindAsync(request.Id);
            return leaveType.ToLeaveTypeDto();
        }
    }
}
