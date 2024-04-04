using AutoMapper;
using HR.LeaveManagement.Application.DTOs;
using HR.LeaveManagement.Application.DTOs.LeaveType;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Queries;
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
    public class GetLeaveTypeListRequestHandler : IAppRequestHandler<GetLeaveTypeListRequest, List<LeaveTypeDto>>
    {
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;
        private readonly IMapper _mapper;

        public GetLeaveTypeListRequestHandler(IGraphRepository<LeaveType> leaveTypeRepository, IMapper mapper)
        {
            _leaveTypeRepository = leaveTypeRepository;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            _mapper = mapper;
        }

        public async Task<List<LeaveTypeDto>> HandleAsync(GetLeaveTypeListRequest request, CancellationToken cancellationToken)
        {
            var leaveTypes = await _leaveTypeRepository.FindAsync(x=> true);
            return _mapper.Map<List<LeaveTypeDto>>(leaveTypes);
        }
    }
}
