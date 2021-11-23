
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RCommon.Persistence.EFCore;
using Samples.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Samples.ObjectAccess.EFCore
{

    public class SamplesContext : RCommonDbContext, ISamplesContext
    {


        private readonly IConfiguration _configuration;

        public SamplesContext()
        {


        }


        public SamplesContext(IConfiguration configuration)
        {

            _configuration = configuration;


        }



        public DbSet<DiveLocation> DiveLocations { get; set; } // DiveLocations
        public DbSet<DiveLocationDetail> DiveLocationDetails { get; set; } // DiveLocationDetails
        public DbSet<DiveType> DiveTypes { get; set; } // DiveTypes
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }





        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured && _configuration != null)

            {

                optionsBuilder.UseSqlServer(_configuration.GetConnectionString(@"Samples"));

            }

        }







        public bool IsSqlParameterNull(SqlParameter param)
        {

            var sqlValue = param.SqlValue;

            var nullableValue = sqlValue as INullable;

            if (nullableValue != null)

                return nullableValue.IsNull;

            return (sqlValue == null || sqlValue == DBNull.Value);

        }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new DiveLocationConfiguration());
            modelBuilder.ApplyConfiguration(new DiveLocationDetailConfiguration());
            modelBuilder.ApplyConfiguration(new DiveTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());


        }
    }
}

