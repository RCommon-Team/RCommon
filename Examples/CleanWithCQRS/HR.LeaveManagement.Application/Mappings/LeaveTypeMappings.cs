using HR.LeaveManagement.Application.DTOs.LeaveType;
using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.Mappings
{
    public static class LeaveTypeMappings
    {
        public static LeaveTypeDto ToLeaveTypeDto(this LeaveType source)
        {
            if (source == null) return null;
            return new LeaveTypeDto
            {
                Id = source.Id,
                Name = source.Name,
                DefaultDays = source.DefaultDays
            };
        }

        public static LeaveType ToLeaveType(this CreateLeaveTypeDto source)
        {
            if (source == null) return null;
            return new LeaveType
            {
                Name = source.Name,
                DefaultDays = source.DefaultDays
            };
        }

        public static void ApplyTo(this LeaveTypeDto source, LeaveType destination)
        {
            if (source == null || destination == null) return;
            destination.Name = source.Name;
            destination.DefaultDays = source.DefaultDays;
        }
    }
}
