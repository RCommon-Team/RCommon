using HR.LeaveManagement.MVC.Models;
using HR.LeaveManagement.MVC.Services.Base;
using System.Collections.Generic;
using System.Linq;

namespace HR.LeaveManagement.MVC.Mappings
{
    public static class ViewModelMappings
    {
        // ─── LeaveType ───────────────────────────────────────────────────────────

        public static LeaveTypeVM ToLeaveTypeVM(this LeaveTypeDto source)
        {
            if (source == null) return null;
            return new LeaveTypeVM
            {
                Id = source.Id,
                Name = source.Name,
                DefaultDays = source.DefaultDays
            };
        }

        public static List<LeaveTypeVM> ToLeaveTypeVMList(this ICollection<LeaveTypeDto> source)
        {
            if (source == null) return new List<LeaveTypeVM>();
            return source.Select(x => x.ToLeaveTypeVM()).ToList();
        }

        public static CreateLeaveTypeDto ToCreateLeaveTypeDto(this CreateLeaveTypeVM source)
        {
            if (source == null) return null;
            return new CreateLeaveTypeDto
            {
                Name = source.Name,
                DefaultDays = source.DefaultDays
            };
        }

        public static LeaveTypeDto ToLeaveTypeDto(this LeaveTypeVM source)
        {
            if (source == null) return null;
            return new LeaveTypeDto
            {
                Id = source.Id,
                Name = source.Name,
                DefaultDays = source.DefaultDays
            };
        }

        // ─── Employee ────────────────────────────────────────────────────────────

        public static EmployeeVM ToEmployeeVM(this Employee source)
        {
            if (source == null) return null;
            return new EmployeeVM
            {
                Id = source.Id,
                Email = source.Email,
                Firstname = source.Firstname,
                Lastname = source.Lastname
            };
        }

        // ─── LeaveRequest ────────────────────────────────────────────────────────

        public static LeaveRequestVM ToLeaveRequestVM(this LeaveRequestDto source)
        {
            if (source == null) return null;
            return new LeaveRequestVM
            {
                Id = source.Id,
                StartDate = source.StartDate.DateTime,
                EndDate = source.EndDate.DateTime,
                DateRequested = source.DateRequested.DateTime,
                DateActioned = source.DateActioned?.DateTime ?? default,
                LeaveTypeId = source.LeaveTypeId,
                LeaveType = source.LeaveType?.ToLeaveTypeVM(),
                Employee = source.Employee?.ToEmployeeVM(),
                RequestComments = source.RequestComments,
                Approved = source.Approved,
                Cancelled = source.Cancelled
            };
        }

        public static LeaveRequestVM ToLeaveRequestVM(this LeaveRequestListDto source)
        {
            if (source == null) return null;
            return new LeaveRequestVM
            {
                Id = source.Id,
                StartDate = source.StartDate.DateTime,
                EndDate = source.EndDate.DateTime,
                DateRequested = source.DateRequested.DateTime,
                LeaveTypeId = source.LeaveType?.Id ?? 0,
                LeaveType = source.LeaveType?.ToLeaveTypeVM(),
                Employee = source.Employee?.ToEmployeeVM(),
                Approved = source.Approved
            };
        }

        public static List<LeaveRequestVM> ToLeaveRequestVMList(this ICollection<LeaveRequestListDto> source)
        {
            if (source == null) return new List<LeaveRequestVM>();
            return source.Select(x => x.ToLeaveRequestVM()).ToList();
        }

        public static CreateLeaveRequestDto ToCreateLeaveRequestDto(this CreateLeaveRequestVM source)
        {
            if (source == null) return null;
            return new CreateLeaveRequestDto
            {
                StartDate = source.StartDate,
                EndDate = source.EndDate,
                LeaveTypeId = source.LeaveTypeId,
                RequestComments = source.RequestComments
            };
        }

        // ─── LeaveAllocation ─────────────────────────────────────────────────────

        public static LeaveAllocationVM ToLeaveAllocationVM(this LeaveAllocationDto source)
        {
            if (source == null) return null;
            return new LeaveAllocationVM
            {
                Id = source.Id,
                NumberOfDays = source.NumberOfDays,
                Period = source.Period,
                LeaveTypeId = source.LeaveTypeId,
                LeaveType = source.LeaveType?.ToLeaveTypeVM()
            };
        }

        public static List<LeaveAllocationVM> ToLeaveAllocationVMList(this ICollection<LeaveAllocationDto> source)
        {
            if (source == null) return new List<LeaveAllocationVM>();
            return source.Select(x => x.ToLeaveAllocationVM()).ToList();
        }

        // ─── Registration ────────────────────────────────────────────────────────

        public static RegistrationRequest ToRegistrationRequest(this RegisterVM source)
        {
            if (source == null) return null;
            return new RegistrationRequest
            {
                FirstName = source.FirstName,
                LastName = source.LastName,
                Email = source.Email,
                UserName = source.UserName,
                Password = source.Password
            };
        }
    }
}
