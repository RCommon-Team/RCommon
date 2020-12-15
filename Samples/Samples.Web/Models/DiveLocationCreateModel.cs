using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Samples.Web.Models
{
    public class DiveLocationCreateModel : DiveLocationModel
    {

        [Required(ErrorMessage = "Please select a file.")]
        [DataType(DataType.Upload)]
        [Display(Name = "Photo")]
        public IFormFile Photo { get; set; }
    }
}
