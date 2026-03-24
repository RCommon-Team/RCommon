using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveAllocations.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands;
using HR.LeaveManagement.Domain;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence;
using RCommon.Persistence.Crud;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands
{
    public class DeleteLeaveAllocationCommandHandler : IAppRequestHandler<DeleteLeaveAllocationCommand>
    {
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;

        public DeleteLeaveAllocationCommandHandler(IGraphRepository<LeaveAllocation> leaveAllocationRepository)
        {
            this._leaveAllocationRepository = leaveAllocationRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task HandleAsync(DeleteLeaveAllocationCommand request, CancellationToken cancellationToken)
        {
            var leaveAllocation = await _leaveAllocationRepository.FindAsync(request.Id);

            if (leaveAllocation == null)
                throw new NotFoundException(nameof(LeaveAllocation), request.Id);

            await _leaveAllocationRepository.DeleteAsync(leaveAllocation);
        }
    }
}
