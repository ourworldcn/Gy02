﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OW.Game.Store;

#nullable disable

namespace UserDb.Migrations
{
    [DbContext(typeof(GY02UserContext))]
    [Migration("20221122062339_22112201")]
    partial class _22112201
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<byte[]>("BinaryArray")
                        .HasColumnType("varbinary(max)");

                    b.Property<decimal?>("ExtraDecimal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ExtraGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ExtraString")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnOrder(10);

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<byte[]>("Timestamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("ExtraGuid", "ExtraDecimal", "ExtraString");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("ExtraGuid", "ExtraDecimal", "ExtraString"), new[] { "ParentId" });

                    b.HasIndex("ExtraGuid", "ExtraString", "ExtraDecimal");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("ExtraGuid", "ExtraString", "ExtraDecimal"), new[] { "ParentId" });

                    b.ToTable("VirtualThings");
                });

            modelBuilder.Entity("OwGameDb.User.OrphanedThing", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<byte[]>("BinaryArray")
                        .HasColumnType("varbinary(max)");

                    b.Property<decimal?>("ExtraDecimal")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid>("ExtraGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ExtraString")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnOrder(10);

                    b.HasKey("Id");

                    b.HasIndex("ExtraGuid", "ExtraDecimal", "ExtraString");

                    b.HasIndex("ExtraGuid", "ExtraString", "ExtraDecimal");

                    b.ToTable("GameUsers");
                });

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.HasOne("OW.Game.Store.VirtualThing", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.Navigation("Children");
                });
#pragma warning restore 612, 618
        }
    }
}
