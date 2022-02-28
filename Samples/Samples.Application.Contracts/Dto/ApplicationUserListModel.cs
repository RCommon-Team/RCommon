using RCommon.Collections;
using RCommon.Models;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Application.Contracts.Dto
{
    public record ApplicationUserListModel : PaginatedListModel<ApplicationUser, ApplicationUserDto>
    {
        public ApplicationUserListModel(IPaginatedList<ApplicationUser> source, PaginatedListRequest paginatedListRequest, int totalCount, int totalPages,
            bool skipTotal = false, bool skipSort = false) 
            : base(source, paginatedListRequest, totalCount, totalPages, skipTotal, skipSort)
        {
        }

        protected override IQueryable<ApplicationUserDto> CastItems(IQueryable<ApplicationUser> source)
        {
            return source.Select(x => new ApplicationUserDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName
            });
        }
    }
}
