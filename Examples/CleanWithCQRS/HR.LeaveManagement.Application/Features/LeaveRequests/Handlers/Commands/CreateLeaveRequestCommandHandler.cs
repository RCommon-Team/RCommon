using AutoMapper;
using HR.LeaveManagement.Application.DTOs.LeaveRequest.Validators;
using HR.LeaveManagement.Application.Exceptions;
using HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands;
using HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands;
using HR.LeaveManagement.Application.Responses;
using HR.LeaveManagement.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HR.LeaveManagement.Application.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using HR.LeaveManagement.Application.Constants;
using RCommon.Persistence;
using RCommon.Security.Users;
using RCommon.Emailing;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RCommon.Emailing.SendGrid;
using RCommon.Persistence.Crud;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Handlers.Commands
{
    public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, BaseCommandResponse>
    {
        private readonly IEmailService _emailSender;
        private readonly ICurrentUser _currentUser;
        private readonly IOptions<SendGridEmailSettings> _emailSettings;
        private readonly IMapper _mapper;
        private readonly IReadOnlyRepository<LeaveType> _leaveTypeRepository;
        private readonly IGraphRepository<LeaveAllocation> _leaveAllocationRepository;
        private readonly IGraphRepository<LeaveRequest> _leaveRequestRepository;

        public CreateLeaveRequestCommandHandler(
            IReadOnlyRepository<LeaveType> leaveTypeRepository,
            IGraphRepository<LeaveAllocation> leaveAllocationRepository,
            IGraphRepository<LeaveRequest> leaveRequestRepository,
            IEmailService emailSender,
            ICurrentUser currentUser,
            IOptions<SendGridEmailSettings> emailSettings,
            IMapper mapper)
        {
            _leaveTypeRepository = leaveTypeRepository;
            _leaveAllocationRepository = leaveAllocationRepository;
            _leaveRequestRepository = leaveRequestRepository;
            this._leaveAllocationRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveTypeRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            this._leaveRequestRepository.DataStoreName = DataStoreNamesConst.LeaveManagement;
            _emailSender = emailSender;
            this._currentUser = currentUser;
            _emailSettings=emailSettings;
            _mapper = mapper;
        }

        public async Task<BaseCommandResponse> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var response = new BaseCommandResponse();
            var validator = new CreateLeaveRequestDtoValidator(_leaveTypeRepository);
            var validationResult = await validator.ValidateAsync(request.LeaveRequestDto);
            var userId = _currentUser.FindClaimValue(CustomClaimTypes.Uid);
            //_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(
                    //q => q.Type == CustomClaimTypes.Uid)?.Value;

            var allocation = _leaveAllocationRepository.FirstOrDefault(x=>x.EmployeeId == userId && x.LeaveTypeId == request.LeaveRequestDto.LeaveTypeId);
            if(allocation is null)
            {
                validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(request.LeaveRequestDto.LeaveTypeId),
                    "You do not have any allocations for this leave type."));
            }
            else
            {
                int daysRequested = (int)(request.LeaveRequestDto.EndDate - request.LeaveRequestDto.StartDate).TotalDays;
                if (daysRequested > allocation.NumberOfDays)
                {
                    validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        nameof(request.LeaveRequestDto.EndDate), "You do not have enough days for this request"));
                }
            }
            
            if (validationResult.IsValid == false)
            {
                response.Success = false;
                response.Message = "Request Failed";
                response.Errors = validationResult.Errors.Select(q => q.ErrorMessage).ToList();
            }
            else
            {
                var leaveRequest = _mapper.Map<LeaveRequest>(request.LeaveRequestDto);
                leaveRequest.RequestingEmployeeId = userId;
                await _leaveRequestRepository.AddAsync(leaveRequest);
                //TODO: May need to get Id out

                response.Success = true;
                response.Message = "Request Created Successfully";
                response.Id = leaveRequest.Id;

                try
                {
                    var emailAddress = _currentUser.FindClaimValue(ClaimTypes.Email);
                    //_httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email).Value;

                    var email = new MailMessage(new MailAddress(this._emailSettings.Value.FromEmailDefault, this._emailSettings.Value.FromNameDefault), 
                        new MailAddress(emailAddress))
                    {
                        Body = $"Your leave request for {request.LeaveRequestDto.StartDate:D} to {request.LeaveRequestDto.EndDate:D} " +
                        $"has been submitted successfully.",
                        Subject = "Leave Request Submitted"
                    };

                    await _emailSender.SendEmailAsync(email);
                }
                catch (Exception ex)
                {
                    //// Log or handle error, but don't throw...
                }
            }
            
            return response;
        }
    }
}
