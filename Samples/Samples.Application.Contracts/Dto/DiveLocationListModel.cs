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
    public record DiveLocationListModel : PaginatedListModel<DiveLocation, DiveLocationDto>
    {
        public DiveLocationListModel(IPaginatedList<DiveLocation> diveLocations, PaginatedListRequest paginatedListRequest, int totalCount, int totalPages, 
            bool skipSort = false) :base(diveLocations, paginatedListRequest, totalCount, skipSort)
        {

        }

        protected override IQueryable<DiveLocationDto> CastItems(IQueryable<DiveLocation> source)
        {
            return source.Select(x => new DiveLocationDto
            {
                Id = x.Id,
                DiveDesc = x.DiveDesc,
                DiveTypeId = x.DiveTypeId,
                DiveTypeName  = x.DiveType.DiveTypeName,
                GpsCoordinates = x.GpsCoordinates,
                ImageData = x.DiveLocationDetail.ImageData,
                LocationName= x.LocationName
            });
        }
    }
}
