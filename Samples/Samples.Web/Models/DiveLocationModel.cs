using Microsoft.AspNetCore.Http;
using Samples.Application.Contracts.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Models
{
    public class DiveLocationModel
    {
        public string DisplayMessage { get; set; }
        public DiveLocationDto DiveLocation { get; set; }

        
    }
}
