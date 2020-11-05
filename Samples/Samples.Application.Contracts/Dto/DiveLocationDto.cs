using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Samples.Application.Contracts.Dto
{
    /// <summary>
    /// Just a simple data transfer object (DTO) for our dive location information.
    /// </summary>
    public class DiveLocationDto
    {
        public DiveLocationDto()
        {
            this.Id = Guid.NewGuid();
        }


        /// <summary>
        /// Id (Primary key)
        /// </summary>
        [Key]
        [Required]
        [Display(Name = "Id")]
        public Guid Id { get; set; }



        /// <summary>
        /// LocationName (length: 255)
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        [Required(AllowEmptyStrings = true)]
        [Display(Name = "Location Name")]
        public string LocationName { get; set; }



        /// <summary>
        /// GpsCoordinates (length: 255)
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        [Required(AllowEmptyStrings = true)]
        [Display(Name = "GPS Coordinates")]
        public string GpsCoordinates { get; set; }



        /// <summary>
        /// DiveTypeId
        /// </summary>
        [Required]
        [Display(Name = "Dive Type")]
        public Guid DiveTypeId { get; set; }

        /// <summary>
        /// DiveTypeName (length: 50)
        /// </summary>

        public string DiveTypeName { get; set; }

        /// <summary>
        /// DiveDesc (length: 1073741823)
        /// </summary>
        [Display(Name = "Dive Description")]
        public string DiveDesc { get; set; }


        /// <summary>
        /// ImageData
        /// </summary>
        [Display(Name = "Image")]
        public byte[] ImageData { get; set; }


    }
}
