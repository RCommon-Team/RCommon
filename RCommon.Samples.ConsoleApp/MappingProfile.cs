using AutoMapper;
using RCommon.Samples.ConsoleApp;
using RCommon.Samples.ConsoleApp.Domain.Entities;
using RCommon.Samples.ConsoleApp.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Samples.ConsoleApp
{
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            CreateMap<CustomerDto, Customer>();
            CreateMap<Customer, CustomerDto>();
        }
    }
}
