using HR.LeaveManagement.MVC.Contracts;
using HR.LeaveManagement.MVC.Mappings;
using HR.LeaveManagement.MVC.Models;
using HR.LeaveManagement.MVC.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.LeaveManagement.MVC.Services
{
    public class LeaveRequestService : BaseHttpService, ILeaveRequestService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly IClient _httpclient;

        public LeaveRequestService(IClient httpclient, ILocalStorageService localStorageService) : base(httpclient, localStorageService)
        {
            this._localStorageService = localStorageService;
            this._httpclient = httpclient;
        }

        public async Task ApproveLeaveRequest(int id, bool approved)
        {
            AddBearerToken();
            try
            {
                var request = new ChangeLeaveRequestApprovalDto { Approved = approved, Id = id };
                await _client.ChangeapprovalAsync(id, request);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Response<int>> CreateLeaveRequest(CreateLeaveRequestVM leaveRequest)
        {
            try
            {
                var response = new Response<int>();
                CreateLeaveRequestDto createLeaveRequest = leaveRequest.ToCreateLeaveRequestDto();
                AddBearerToken();
                var apiResponse = await _client.LeaveRequestsPOSTAsync(createLeaveRequest);
                if (apiResponse.Success)
                {
                    response.Data = apiResponse.Id;
                    response.Success = true;
                }
                else
                {
                    foreach (var error in apiResponse.Errors)
                    {
                        response.ValidationErrors += error + Environment.NewLine;
                    }
                }
                return response;
            }
            catch (ApiException ex)
            {
                return ConvertApiExceptions<int>(ex);
            }
        }

        public Task DeleteLeaveRequest(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AdminLeaveRequestViewVM> GetAdminLeaveRequestList()
        {
            AddBearerToken();
            var leaveRequests = await _client.LeaveRequestsAllAsync(isLoggedInUser: false);

            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequests.Count,
                ApprovedRequests = leaveRequests.Count(q => q.Approved == true),
                PendingRequests = leaveRequests.Count(q => q.Approved == null),
                RejectedRequests = leaveRequests.Count(q => q.Approved == false),
                LeaveRequests = leaveRequests.ToLeaveRequestVMList()
            };
            return model;
        }

        public async Task<LeaveRequestVM> GetLeaveRequest(int id)
        {
            AddBearerToken();
            var leaveRequest = await _client.LeaveRequestsGETAsync(id);
            return leaveRequest.ToLeaveRequestVM();
        }

        public async Task<EmployeeLeaveRequestViewVM> GetUserLeaveRequests()
        {
            AddBearerToken();
            var leaveRequests = await _client.LeaveRequestsAllAsync(isLoggedInUser: true);
            var allocations = await _client.LeaveAllocationsAllAsync(isLoggedInUser: true);
            var model = new EmployeeLeaveRequestViewVM
            {
                LeaveAllocations = allocations.ToLeaveAllocationVMList(),
                LeaveRequests = leaveRequests.ToLeaveRequestVMList()
            };

            return model;
        }
    }
}
