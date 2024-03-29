﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RCommon.TestBase.Data;

#nullable disable

namespace RCommon.TestBase.Data.Migrations
{
    [DbContext(typeof(TestDbContext))]
    [Migration("20220830040850_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("RCommon.TestBase.Entities.Customer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("CustomerID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("City")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("City");

                    b.Property<string>("FirstName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("FirstName");

                    b.Property<string>("LastName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("LastName");

                    b.Property<string>("State")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("State");

                    b.Property<string>("StreetAddress1")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("StreetAddress1");

                    b.Property<string>("StreetAddress2")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("StreetAddress2");

                    b.Property<string>("ZipCode")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("ZipCode");

                    b.HasKey("Id")
                        .HasName("PK__Customer__A4AE64B8BBC282A0");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.ToTable("Customers", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Department", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("Id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Name");

                    b.HasKey("Id")
                        .HasName("PK__Departme__3214EC07CB60603A");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.ToTable("Departments", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.MonthlySalesSummary", b =>
                {
                    b.Property<int>("Year")
                        .HasColumnType("int")
                        .HasColumnName("Year");

                    b.Property<int>("Month")
                        .HasColumnType("int")
                        .HasColumnName("Month");

                    b.Property<int>("SalesPersonId")
                        .HasColumnType("int")
                        .HasColumnName("SalesPersonId");

                    b.Property<decimal?>("Amount")
                        .HasColumnType("decimal(19,5)")
                        .HasColumnName("Amount");

                    b.Property<string>("Currency")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Currency");

                    b.Property<string>("SalesPersonFirstName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("SalesPersonFirstName");

                    b.Property<string>("SalesPersonLastName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("SalesPersonLastName");

                    b.HasKey("Year", "Month", "SalesPersonId")
                        .HasName("PK__MonthlyS__C26E735F2ADBDAB9");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Year", "Month", "SalesPersonId"));

                    b.ToTable("MonthlySalesSummary", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Order", b =>
                {
                    b.Property<int>("OrderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("OrderID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("OrderId"), 1L, 1);

                    b.Property<int?>("CustomerId")
                        .HasColumnType("int")
                        .HasColumnName("CustomerId");

                    b.Property<DateTime?>("OrderDate")
                        .HasColumnType("datetime")
                        .HasColumnName("OrderDate");

                    b.Property<DateTime?>("ShipDate")
                        .HasColumnType("datetime")
                        .HasColumnName("ShipDate");

                    b.HasKey("OrderId")
                        .HasName("PK__Orders__C3905BAF964CE0E8");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("OrderId"));

                    b.HasIndex("CustomerId");

                    b.ToTable("Orders", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.OrderItem", b =>
                {
                    b.Property<int>("OrderItemId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("OrderItemID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("OrderItemId"), 1L, 1);

                    b.Property<int?>("OrderId")
                        .HasColumnType("int")
                        .HasColumnName("OrderId");

                    b.Property<decimal?>("Price")
                        .HasColumnType("decimal(19,5)")
                        .HasColumnName("Price");

                    b.Property<int?>("ProductId")
                        .HasColumnType("int")
                        .HasColumnName("ProductId");

                    b.Property<int?>("Quantity")
                        .HasColumnType("int")
                        .HasColumnName("Quantity");

                    b.Property<string>("Store")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Store");

                    b.HasKey("OrderItemId")
                        .HasName("PK__OrderIte__57ED06A1FD2C18E0");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("OrderItemId"));

                    b.HasIndex("OrderId");

                    b.HasIndex("ProductId");

                    b.ToTable("OrderItems", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Product", b =>
                {
                    b.Property<int>("ProductId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("ProductID");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProductId"), 1L, 1);

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Description");

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Name");

                    b.HasKey("ProductId")
                        .HasName("PK__Products__B40CC6EDA7CB3960");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("ProductId"));

                    b.ToTable("Products", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.SalesPerson", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("Id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<int?>("DepartmentId")
                        .HasColumnType("int")
                        .HasColumnName("DepartmentId");

                    b.Property<string>("FirstName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("FirstName");

                    b.Property<string>("LastName")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("LastName");

                    b.Property<float?>("SalesQuota")
                        .HasColumnType("real")
                        .HasColumnName("SalesQuota");

                    b.Property<decimal?>("SalesYtd")
                        .HasColumnType("decimal(19,5)")
                        .HasColumnName("SalesYTD");

                    b.Property<int?>("TerritoryId")
                        .HasColumnType("int")
                        .HasColumnName("TerritoryId");

                    b.HasKey("Id")
                        .HasName("PK__SalesPer__3214EC0722B792EE");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.HasIndex("DepartmentId");

                    b.HasIndex("TerritoryId");

                    b.ToTable("SalesPerson", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.SalesTerritory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("Id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Description");

                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("Name");

                    b.HasKey("Id")
                        .HasName("PK__SalesTer__3214EC071C7A3190");

                    SqlServerKeyBuilderExtensions.IsClustered(b.HasKey("Id"));

                    b.ToTable("SalesTerritory", "dbo");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Order", b =>
                {
                    b.HasOne("RCommon.TestBase.Entities.Customer", "Customer")
                        .WithMany("Orders")
                        .HasForeignKey("CustomerId")
                        .HasConstraintName("FK_Customer_Orders");

                    b.Navigation("Customer");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.OrderItem", b =>
                {
                    b.HasOne("RCommon.TestBase.Entities.Order", "Order")
                        .WithMany("OrderItems")
                        .HasForeignKey("OrderId")
                        .HasConstraintName("FK_Orders_OrderItems");

                    b.HasOne("RCommon.TestBase.Entities.Product", "Product")
                        .WithMany("OrderItems")
                        .HasForeignKey("ProductId")
                        .HasConstraintName("FK_OrderItems_Product");

                    b.Navigation("Order");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.SalesPerson", b =>
                {
                    b.HasOne("RCommon.TestBase.Entities.Department", "Department")
                        .WithMany("SalesPersons")
                        .HasForeignKey("DepartmentId")
                        .HasConstraintName("FK74214A90E25FF6");

                    b.HasOne("RCommon.TestBase.Entities.SalesTerritory", "SalesTerritory")
                        .WithMany("SalesPersons")
                        .HasForeignKey("TerritoryId")
                        .HasConstraintName("FK74214A90B23DB0A3");

                    b.Navigation("Department");

                    b.Navigation("SalesTerritory");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Customer", b =>
                {
                    b.Navigation("Orders");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Department", b =>
                {
                    b.Navigation("SalesPersons");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Order", b =>
                {
                    b.Navigation("OrderItems");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.Product", b =>
                {
                    b.Navigation("OrderItems");
                });

            modelBuilder.Entity("RCommon.TestBase.Entities.SalesTerritory", b =>
                {
                    b.Navigation("SalesPersons");
                });
#pragma warning restore 612, 618
        }
    }
}
