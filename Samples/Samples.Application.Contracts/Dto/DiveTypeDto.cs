using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Samples.Application.Contracts.Dto
{
    public class DiveTypeDto
    {
        /// <summary>
        /// Id (Primary key)
        /// </summary>
        [Key]
        [Required]
        [Display(Name = "Id")]
        public Guid Id { get; set; }



        /// <summary>
        /// DiveTypeName (length: 50)
        /// </summary>
        [MaxLength(50)]
        [StringLength(50)]
        [Required(AllowEmptyStrings = true)]
        [Display(Name = "Dive Type Name")]
        public string DiveTypeName { get; set; }



        /// <summary>
        /// DiveTypeDesc (length: 1073741823)
        /// </summary>



        [MaxLength]
        [Required(AllowEmptyStrings = true)]
        [Display(Name = "Dive Type Description")]
        public string DiveTypeDesc { get; set; }
    }
}
