
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Samples.ObjectAccess.EFCore
{

    public interface ISamplesContext

    {
        DbSet<DiveLocation> DiveLocations { get; set; } // DiveLocations
        DbSet<DiveLocationDetail> DiveLocationDetails { get; set; } // DiveLocationDetails
        DbSet<DiveType> DiveTypes { get; set; } // DiveTypes

    }
}


