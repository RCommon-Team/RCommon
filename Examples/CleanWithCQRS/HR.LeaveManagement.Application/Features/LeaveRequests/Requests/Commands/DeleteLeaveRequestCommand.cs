using RCommon.Mediator.Subscribers;
using System;
using System.Collections.Generic;
using System.Text;

namespace HR.LeaveManagement.Application.Features.LeaveRequests.Requests.Commands
{
    public class DeleteLeaveRequestCommand : IAppRequest
    {
        public int Id { get; set; }
    }
}
