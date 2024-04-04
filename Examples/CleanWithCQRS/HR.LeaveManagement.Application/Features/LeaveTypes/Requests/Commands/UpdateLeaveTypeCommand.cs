using HR.LeaveManagement.Application.DTOs.LeaveType;
using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;

namespace HR.LeaveManagement.Application.Features.LeaveTypes.Requests.Commands
{
    public class UpdateLeaveTypeCommand : IAppRequest
    {
        public LeaveTypeDto LeaveTypeDto { get; set; }

    }
}
