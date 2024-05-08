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
using HR.LeaveManagement.Application.Responses;
using System.Linq;
using HR.LeaveManagement.Application.Contracts.Identity;
using RCommon.Persistence;
using HR.LeaveManagement.Domain.Specifications;
using RCommon.Persistence.Crud;
using RCommon.ApplicationServices.Validation;

namespace HR.LeaveManagement.Application.Features.LeaveAllocations.Handlers.Commands
{
    public class CreateLeaveAllocationCommandHandler : IAppRequestHandler<CreateLeaveAllocationCommand, BaseCommandResponse>
    {
        private readonly IGraphRepository<LeaveType> _leaveTypeRepository;
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidationService _validationService;

        public CreateLeaveAllocationCommandHandler(IGraphRepository<LeaveType> leaveTypeRepository,
            IGraphRepository<LeaveAllocation> leaveAllocationRepository,
            IUserService userService,
            IMapper mapper, 
            IValidationService validationService)
        {
            this._leaveTypeRepository = leaveTypeRepository;
            this._leaveAllocationRepository = leaveAllocationRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._userService = userService;
            _mapper = mapper;
            _validationService = validationService;
        }

        public async Task<BaseCommandResponse> HandleAsync(CreateLeaveAllocationCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse();
            var validationResult = await _validationService.ValidateAsync(request.LeaveAllocationDto);

            if (validationResult.IsValid == false)
            {
                response.Success = false;
                response.Message = "Allocations Failed";
                response.Errors = validationResult.Errors.Select(q => q.ErrorMessage).ToList();
            }
            else
            {
                var leaveType = await _leaveTypeRepository.FindAsync(request.LeaveAllocationDto.LeaveTypeId);
                var employees = await _userService.GetEmployees();
                var period = DateTime.Now.Year;
                var allocations = new List<LeaveAllocation>();
                foreach (var emp in employees)
                {
                    var allocationCount = await _leaveAllocationRepository.GetCountAsync(new AllocationExistsSpec(emp.Id, leaveType.Id, period));
                    if (allocationCount > 0)
                        continue;
                    allocations.Add(new LeaveAllocation
                    {
                        EmployeeId = emp.Id,
                        LeaveTypeId = leaveType.Id,
                        NumberOfDays = leaveType.DefaultDays,
                        Period = period
                    });
                }
                foreach (var item in allocations)
                {
                    await _leaveAllocationRepository.AddAsync(item);
                }
                
                response.Success = true;
                response.Message = "Allocations Successful";
            }


            return response;
        }
    }
}
