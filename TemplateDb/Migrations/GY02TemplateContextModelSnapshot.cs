﻿// <auto-generated />
using System;
using GuangYuan.GY001.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    [DbContext(typeof(GY02TemplateContext))]
    partial class GY02TemplateContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("GuangYuan.GY001.TemplateDb.GameItemTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<string>("ChildrenTemplateIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("GId")
                        .HasColumnType("int");

                    b.Property<string>("GenusIdString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("备注")
                        .HasColumnOrder(90);

                    b.Property<string>("Script")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ItemTemplates");
                });
#pragma warning restore 612, 618
        }
    }
}
