using AutoMapper;
using RCommon.Collections;
using Samples.Application.Contracts.Dto;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Samples.Application
{
    public class ApplicationLayerMappingProfile : Profile
    {
        public ApplicationLayerMappingProfile()
        {

            CreateMap<DiveLocation, DiveLocationDto>()
            .ForMember(x => x.Id, m => m.MapFrom(f => f.Id))
            .ForMember(x => x.LocationName, m => m.MapFrom(f => f.LocationName))
            .ForMember(x => x.DiveDesc, m => m.MapFrom(f => f.DiveDesc))
            .ForMember(x => x.DiveTypeId, m => m.MapFrom(f => f.DiveTypeId))
            .ForPath(x => x.ImageData, m => m.MapFrom(f => f.DiveLocationDetail.ImageData))
            .ForPath(x => x.DiveTypeName, m => m.MapFrom(f => f.DiveType.DiveTypeName));

            CreateMap<DiveLocationDto, DiveLocation>()
            .ForMember(x => x.Id, m => m.MapFrom(f => f.Id))
            .ForMember(x => x.LocationName, m => m.MapFrom(f => f.LocationName))
            .ForMember(x => x.DiveDesc, m => m.MapFrom(f => f.DiveDesc))
            .ForMember(x => x.DiveTypeId, m => m.MapFrom(f => f.DiveTypeId));

            CreateMap<ICollection<DiveLocation>, ICollection<DiveLocationDto>>();
            CreateMap<DiveType, DiveTypeDto>();
            CreateMap<ICollection<DiveType>, ICollection<DiveTypeDto>>();
            CreateMap<ApplicationUser, ApplicationUserDto> ();
        }

        

    }
}
