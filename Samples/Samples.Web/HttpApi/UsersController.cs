using Microsoft.AspNetCore.Mvc;
using Samples.Application.ApplicationServices;
using Samples.Application.Contracts.Dto;
using Samples.Domain;
using Samples.Domain.Entities;
using Samples.Web.Infrastructure;
using Samples.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Samples.Web.HttpApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IApplicationUserService _userService;

        public UsersController(IApplicationUserService userService)
        {
            _userService = userService;
        }


        // GET: api/<UsersController>
        [HttpGet]
        public async Task<JsonResult> SearchUsers(string q, int page = 1)
        {
            try
            {
                var request = new ApplicationUserSearchRequest();
                request.SearchString = q;
                request.PageSize = page;
                request.PageSize = PresentationDefaults.PagedDataSize;
                var result = await _userService.SearchUsersAsync(request);
                return new JsonResult(result);
            }
            catch (ApplicationException ex)
            {

                throw ex;
            }
        }

    }
}
