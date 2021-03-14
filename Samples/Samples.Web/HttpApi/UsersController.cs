using Microsoft.AspNetCore.Mvc;
using RCommon.Domain.Repositories;
using Samples.Application.ApplicationServices;
using Samples.Domain;
using Samples.Domain.Entities;
using Samples.Web.Infrastructure;
using Samples.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
                var result = await _userService.SearchUsersAsync(q, page, PresentationDefaults.PagedDataSize);
                return new JsonResult(result);
            }
            catch (ApplicationException ex)
            {

                throw ex;
            }
        }

    }
}
