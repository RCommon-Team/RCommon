using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveType.Validators;
using HR.LeaveManagement.Application.Exceptions;
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

namespace HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Commands
{
    public class UpdateLeaveTypeCommandHandler : IAppRequestHandler<UpdateLeaveTypeCommand>
    {
        private readonly IMapper _mapper;
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;
        private readonly IValidationService _validationService;

        public UpdateLeaveTypeCommandHandler(IMapper mapper, IGraphRepository<LeaveType> leaveTypeRepository, IValidationService validationService)
        {
            _mapper = mapper;
            _leaveTypeRepository = leaveTypeRepository;
            _validationService = validationService;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task HandleAsync(UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validationService.ValidateAsync(request.LeaveTypeDto);

            if (validationResult.IsValid == false)
                throw new ValidationException(validationResult.Errors);

            var leaveType = await _leaveTypeRepository.FindAsync(request.LeaveTypeDto.Id);

            if (leaveType is null)
                throw new NotFoundException(nameof(leaveType), request.LeaveTypeDto.Id);

            _mapper.Map(request.LeaveTypeDto, leaveType);

            await _leaveTypeRepository.UpdateAsync(leaveType);

            
        }
    }
}
