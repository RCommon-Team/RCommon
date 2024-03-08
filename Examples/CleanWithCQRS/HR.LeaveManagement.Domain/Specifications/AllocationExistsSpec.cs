using RCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HR.LeaveManagement.Domain.Specifications
{
    public class AllocationExistsSpec : Specification<LeaveAllocation>
    {
        public AllocationExistsSpec(string userId, int leaveTypeId, int period) : 
            base(q => q.EmployeeId == userId
                                        && q.LeaveTypeId == leaveTypeId
                                        && q.Period == period)
        {
        }
    }
}
