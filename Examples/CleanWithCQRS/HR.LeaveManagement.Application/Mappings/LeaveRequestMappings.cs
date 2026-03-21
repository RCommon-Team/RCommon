using HR.LeaveManagement.Application.DTOs.LeaveRequest;
using HR.LeaveManagement.Domain;
using System;

namespace HR.LeaveManagement.Application.Mappings
{
    public static class LeaveRequestMappings
    {
        public static LeaveRequestDto ToLeaveRequestDto(this LeaveRequest source)
        {
            if (source == null) return null;
            return new LeaveRequestDto
            {
                Id = source.Id,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                LeaveTypeId = source.LeaveTypeId,
                LeaveType = source.LeaveType?.ToLeaveTypeDto(),
                DateRequested = source.DateRequested,
                RequestComments = source.RequestComments,
                DateActioned = source.DateActioned,
                Approved = source.Approved,
                Cancelled = source.Cancelled,
                RequestingEmployeeId = source.RequestingEmployeeId
            };
        }

        public static LeaveRequestListDto ToLeaveRequestListDto(this LeaveRequest source)
        {
            if (source == null) return null;
            return new LeaveRequestListDto
            {
                Id = source.Id,
                RequestingEmployeeId = source.RequestingEmployeeId,
                LeaveType = source.LeaveType?.ToLeaveTypeDto(),
                // DateRequested maps from DateCreated (audit field) per MappingProfile
                DateRequested = source.DateCreated ?? DateTime.MinValue,
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                Approved = source.Approved
            };
        }

        public static LeaveRequest ToLeaveRequest(this CreateLeaveRequestDto source)
        {
            if (source == null) return null;
            return new LeaveRequest
            {
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                LeaveTypeId = source.LeaveTypeId,
                RequestComments = source.RequestComments
            };
        }

        public static void ApplyTo(this UpdateLeaveRequestDto source, LeaveRequest destination)
        {
            if (source == null || destination == null) return;
            destination.StartDate = source.StartDate;
            destination.EndDate = source.EndDate;
            destination.LeaveTypeId = source.LeaveTypeId;
            destination.RequestComments = source.RequestComments;
            destination.Cancelled = source.Cancelled;
        }
    }
}
