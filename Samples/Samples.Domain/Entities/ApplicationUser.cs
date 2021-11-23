﻿// ------------------------------------------------------------------------------------------------

// <auto-generated>
// ReSharper disable CheckNamespace
// ReSharper disable ConvertPropertyToExpressionBody
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable EmptyNamespace
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedVariable
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantCast
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantOverridenMember
// ReSharper disable UseNameofExpression
// ReSharper disable UsePatternMatching
#pragma warning disable 1591    //  Ignore "Missing XML Comment" warning



using RCommon.BusinessEntities;
using System;


using System.Collections.Generic;


using System.ComponentModel.DataAnnotations;


using System.Threading;


using System.Threading.Tasks;


namespace Samples.Domain.Entities
{





    // AspNetUsers



    public partial class ApplicationUser : BusinessEntity

    {


        /// <summary>

        /// Id (Primary key) (length: 450)

        /// </summary>



        [Key]




        [Display(Name = "Id")]


        public int Id { get; set; }





        /// <summary>

        /// FirstName (length: 50)

        /// </summary>



        [MaxLength(50)]


        [StringLength(50)]


        [Display(Name = "First name")]


        public string FirstName { get; set; }



        /// <summary>

        /// LastName (length: 50)

        /// </summary>



        [MaxLength(50)]


        [StringLength(50)]


        [Display(Name = "Last name")]


        public string LastName { get; set; }



        public ApplicationUser()

        {

        }

        public override object[] GetKeys()
        {
            return new object[] { this.Id };
        }
    }

}
// </auto-generated>
