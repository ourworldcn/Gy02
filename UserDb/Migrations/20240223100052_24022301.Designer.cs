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
    [Migration("20240223100052_24022301")]
    partial class _24022301
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("OW.Game.Store.Base.GameRedeemCode", b =>
                {
                    b.Property<string>("Code")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)")
                        .HasColumnOrder(0)
                        .HasComment("兑换码，也是Id。");

                    b.Property<Guid>("CatalogId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Count")
                        .HasColumnType("int");

                    b.HasKey("Code");

                    b.ToTable("GameRedeemCodes");
                });

            modelBuilder.Entity("OW.Game.Store.Base.GameRedeemCodeCatalog", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<int>("CodeType")
                        .HasColumnType("int")
                        .HasComment("生成的码的类型，1=通用码，2=一次性码。");

                    b.Property<string>("DisplayName")
                        .HasColumnType("nvarchar(max)")
                        .HasComment("显示名称");

                    b.Property<Guid>("ShoppingTId")
                        .HasColumnType("uniqueidentifier")
                        .HasComment("兑换码使用的商品TId");

                    b.HasKey("Id");

                    b.ToTable("GameRedeemCodeCatalogs");
                });

            modelBuilder.Entity("OW.Game.Store.GameShoppingOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<byte[]>("BinaryArray")
                        .HasColumnType("varbinary(max)");

                    b.Property<DateTime?>("CompletionDateTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("Confirm1")
                        .HasColumnType("bit");

                    b.Property<bool>("Confirm2")
                        .HasColumnType("bit");

                    b.Property<DateTime>("CreateUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Currency")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CustomerId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("varchar(max)")
                        .HasColumnOrder(10);

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ShoppingOrder");
                });

            modelBuilder.Entity("OW.Game.Store.GameShoppingOrderDetail", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<byte[]>("BinaryArray")
                        .HasColumnType("varbinary(max)");

                    b.Property<decimal>("Count")
                        .HasColumnType("decimal(18,2)");

                    b.Property<Guid?>("GameShoppingOrderId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("GoodsId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("GameShoppingOrderId");

                    b.ToTable("GameShoppingOrderDetail");
                });

            modelBuilder.Entity("OW.Game.Store.ServerConfigItem", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<DateTime>("LastModifyUtc")
                        .HasColumnType("datetime2");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Name");

                    b.ToTable("ServerConfig");
                });

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnOrder(0);

                    b.Property<byte[]>("BinaryArray")
                        .HasColumnType("varbinary(max)");

                    b.Property<DateTime?>("ExtraDateTime")
                        .HasColumnType("datetime2")
                        .HasComment("记录扩展的日期时间属性");

                    b.Property<decimal?>("ExtraDecimal")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal(18,4)")
                        .HasComment("记录一些额外的数值信息，用于排序搜索使用的字段");

                    b.Property<Guid>("ExtraGuid")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ExtraString")
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)")
                        .HasComment("记录一些额外的信息，通常这些信息用于排序，加速查找符合特定要求的对象");

                    b.Property<string>("JsonObjectString")
                        .HasColumnType("varchar(max)")
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

            modelBuilder.Entity("OW.Game.Store.GameShoppingOrderDetail", b =>
                {
                    b.HasOne("OW.Game.Store.GameShoppingOrder", null)
                        .WithMany("Detailes")
                        .HasForeignKey("GameShoppingOrderId");
                });

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.HasOne("OW.Game.Store.VirtualThing", "Parent")
                        .WithMany("Children")
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("OW.Game.Store.GameShoppingOrder", b =>
                {
                    b.Navigation("Detailes");
                });

            modelBuilder.Entity("OW.Game.Store.VirtualThing", b =>
                {
                    b.Navigation("Children");
                });
#pragma warning restore 612, 618
        }
    }
}
