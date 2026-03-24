using HR.LeaveManagement.Application.DTOs.LeaveType.Validators;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands;
using HR.LeaveManagement.Application.Mappings;
using HR.LeaveManagement.Domain;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HR.LeaveManagement.Application.Responses;
using System.Linq;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.ApplicationServices.Validation;

namespace HR.LeaveManagement.Application.Features.LeaveTypes.Handlers.Commands
{
    public class CreateLeaveTypeCommandHandler : IAppRequestHandler<CreateLeaveTypeCommand, BaseCommandResponse>
    {
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;
        private readonly IValidationService _validationService;

        public CreateLeaveTypeCommandHandler(IGraphRepository<LeaveType> leaveTypeRepository, IValidationService validationService)
        {
            _leaveTypeRepository = leaveTypeRepository;
            _validationService = validationService;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
        }

        public async Task<BaseCommandResponse> HandleAsync(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse();
            var validationResult = await _validationService.ValidateAsync(request.LeaveTypeDto);

            if (validationResult.IsValid == false)
            {
                response.Success = false;
                response.Message = "Creation Failed";
                response.Errors = validationResult.Errors.Select(q => q.ErrorMessage).ToList();
            }
            else
            {
                var leaveType = request.LeaveTypeDto.ToLeaveType();

                await _leaveTypeRepository.AddAsync(leaveType);

                // TODO: may need to get id

                response.Success = true;
                response.Message = "Creation Successful";
                response.Id = leaveType.Id;
            }

            return response;
        }
    }
}
