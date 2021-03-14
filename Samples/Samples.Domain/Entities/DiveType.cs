// ------------------------------------------------------------------------------------------------

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



using System;


using System.Collections.Generic;


using System.ComponentModel.DataAnnotations;


using System.Threading;


using System.Threading.Tasks;


namespace Samples.Domain.Entities
{
    




// DiveTypes
    


public partial class DiveType
    
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
    

    [Display(Name = "Dive type name")]
    

    public string DiveTypeName { get; set; }

    

    /// <summary>
    
    /// DiveTypeDesc (length: 1073741823)
    
    /// </summary>
    


    [MaxLength]
    

    [Required(AllowEmptyStrings = true)]
    

    [Display(Name = "Dive type desc")]
    

    public string DiveTypeDesc { get; set; }

    
    // Reverse navigation

    
    /// <summary>
    
    /// Child DiveLocations where [DiveLocations].[DiveTypeId] point to this entity (FK_DiveLocations_DiveTypes)
    
    /// </summary>
    






    public virtual ICollection<DiveLocation> DiveLocations { get; set; } // DiveLocations.FK_DiveLocations_DiveTypes

    
    public DiveType()
    
    {
    


        Id = Guid.NewGuid();
    



        DiveLocations = new List<DiveLocation>();
    



        InitializePartial();
    


    }

    
    partial void InitializePartial();
    




}

}
// </auto-generated>
