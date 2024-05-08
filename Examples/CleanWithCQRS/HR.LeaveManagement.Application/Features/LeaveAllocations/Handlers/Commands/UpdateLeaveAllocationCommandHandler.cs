using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveAllocation.Validators;
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
using RCommon.ApplicationServices.Validation;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands
{
    public class UpdateLeaveAllocationCommandHandler : IAppRequestHandler<UpdateLeaveAllocationCommand>
    {
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;
        private readonly IReadOnlyRepository<LeaveType> _leaveTypeRepository;
        private readonly IMapper _mapper;
        private readonly IValidationService _validationService;

        public UpdateLeaveAllocationCommandHandler(IGraphRepository<LeaveAllocation> leaveAllocationRepository,
            IReadOnlyRepository<LeaveType> leaveTypeRepository,
            IMapper mapper,
            IValidationService validationService)
        {
            this._leaveAllocationRepository = leaveAllocationRepository;
            this._leaveTypeRepository = leaveTypeRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            _mapper = mapper;
            _validationService = validationService;
        }

        public async Task HandleAsync(UpdateLeaveAllocationCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validationService.ValidateAsync(request.LeaveAllocationDto);

            if (validationResult.IsValid == false)
                throw new ValidationException(validationResult.Errors);

            var leaveAllocation = await _leaveAllocationRepository.FindAsync(request.LeaveAllocationDto.Id);

            if (leaveAllocation is null)
                throw new NotFoundException(nameof(leaveAllocation), request.LeaveAllocationDto.Id);

            _mapper.Map(request.LeaveAllocationDto, leaveAllocation);

            await _leaveAllocationRepository.UpdateAsync(leaveAllocation);
            
        }
    }
}
