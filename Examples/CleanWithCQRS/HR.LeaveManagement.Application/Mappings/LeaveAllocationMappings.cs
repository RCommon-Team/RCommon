using HR.LeaveManagement.Application.DTOs.LeaveAllocation;
using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.Mappings
{
    public static class LeaveAllocationMappings
    {
        public static LeaveAllocationDto ToLeaveAllocationDto(this LeaveAllocation source)
        {
            if (source == null) return null;
            return new LeaveAllocationDto
            {
                Id = source.Id,
                NumberOfDays = source.NumberOfDays,
                LeaveTypeId = source.LeaveTypeId,
                LeaveType = source.LeaveType?.ToLeaveTypeDto(),
                Period = source.Period,
                EmployeeId = source.EmployeeId
            };
        }

        public static void ApplyTo(this UpdateLeaveAllocationDto source, LeaveAllocation destination)
        {
            if (source == null || destination == null) return;
            destination.NumberOfDays = source.NumberOfDays;
            destination.LeaveTypeId = source.LeaveTypeId;
            destination.Period = source.Period;
        }
    }
}
