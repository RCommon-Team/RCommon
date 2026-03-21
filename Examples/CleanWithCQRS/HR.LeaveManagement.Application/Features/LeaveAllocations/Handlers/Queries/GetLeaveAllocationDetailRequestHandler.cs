using HR.LeaveManagement.Application.DTOs;
using HR.LeaveManagement.Application.DTOs.LeaveAllocation;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Queries;
using HR.LeaveManagement.Application.Mappings;
using HR.LeaveManagement.Domain;
using RCommon.Mediator.Subscribers;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using System.Threading;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Queries
{
    public class GetLeaveAllocationDetailRequestHandler : IAppRequestHandler<GetLeaveAllocationDetailRequest, LeaveAllocationDto>
    {
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;

        public GetLeaveAllocationDetailRequestHandler(IGraphRepository<LeaveAllocation> leaveAllocationRepository)
        {
            _leaveAllocationRepository = leaveAllocationRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task<LeaveAllocationDto> HandleAsync(GetLeaveAllocationDetailRequest request, CancellationToken cancellationToken)
        {
            _leaveAllocationRepository.Include(x => x.LeaveType);
            var leaveAllocation = await _leaveAllocationRepository.FindAsync(request.Id);
            return leaveAllocation.ToLeaveAllocationDto();
        }
    }
}
