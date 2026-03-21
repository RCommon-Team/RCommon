using HR.LeaveManagement.Application.DTOs;
using HR.LeaveManagement.Application.DTOs.LeaveType;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
using HR.LeaveManagement.Application.Mappings;
using HR.LeaveManagement.Domain;
using RCommon.Mediator.Subscribers;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Queries
{
    public class GetLeaveTypeListRequestHandler : IAppRequestHandler<GetLeaveTypeListRequest, List<LeaveTypeDto>>
    {
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;

        public GetLeaveTypeListRequestHandler(IGraphRepository<LeaveType> leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task<List<LeaveTypeDto>> HandleAsync(GetLeaveTypeListRequest request, CancellationToken cancellationToken)
        {
            var leaveTypes = await _leaveTypeRepository.FindAsync(x => true);
            return leaveTypes.Select(x => x.ToLeaveTypeDto()).ToList();
        }
    }
}
