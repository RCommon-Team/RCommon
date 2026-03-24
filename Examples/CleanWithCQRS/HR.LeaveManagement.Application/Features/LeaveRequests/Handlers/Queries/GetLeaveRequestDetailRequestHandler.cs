using HR.LeaveManagement.Application.DTOs;
using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Queries;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
using HR.LeaveManagement.Application.Mappings;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HR.LeaveManagement.Application.Contracts.Identity;
using RCommon.Persistence;
using HR.LeaveManagement.Domain;
using RCommon.Persistence.Crud;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Queries
{
    public class GetLeaveRequestDetailRequestHandler : IAppRequestHandler<GetLeaveRequestDetailRequest, LeaveRequestDto>
    {
        private readonly IGraphRepository<LeaveRequest> _leaveRequestRepository;
        private readonly IUserService _userService;

        public GetLeaveRequestDetailRequestHandler(IGraphRepository<LeaveRequest> leaveRequestRepository,
            IUserService userService)
        {
            _leaveRequestRepository = leaveRequestRepository;
            this._leaveRequestRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._userService = userService;
        }

        public async Task<LeaveRequestDto> HandleAsync(GetLeaveRequestDetailRequest request, CancellationToken cancellationToken)
        {
            _leaveRequestRepository.Include(x => x.LeaveType);
            var leaveRequestDto = (await _leaveRequestRepository.FindAsync(request.Id)).ToLeaveRequestDto();
            leaveRequestDto.Employee = await _userService.GetEmployee(leaveRequestDto.RequestingEmployeeId);
            return leaveRequestDto;
        }
    }
}
