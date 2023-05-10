﻿// <auto-generated />
using System;
using GY02.TemplateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GuangYuan.GY001.TemplateDb.Migrations
{
    [DbContext(typeof(GY02TemplateContext))]
    [Migration("20221122062227_22112201")]
    partial class _22112201
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("GuangYuan.GY02.Store.GY02ThingTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<long?>("ExtraLong")
                        .HasColumnType("bigint")
                        .HasColumnOrder(21)
                        .HasComment("扩展的长整型信息。");

                    b.Property<string>("GenusString")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnOrder(20)
                        .HasComment("属信息，逗号分隔的字符串。");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnOrder(10);

                    b.Property<string>("Remark")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnOrder(90)
                        .HasComment("注释信息，服务器不使用该字段。");

                    b.HasKey("Id");

                    b.ToTable("ThingTemplates");
                });
#pragma warning restore 612, 618
        }
    }
}
