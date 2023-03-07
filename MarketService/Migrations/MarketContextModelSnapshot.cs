﻿// <auto-generated />
using System;
using MarketService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketService.Migrations
{
    [DbContext(typeof(MarketContext))]
    partial class MarketContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("MarketService.Models.ProductModel", b =>
                {
                    b.Property<Guid>("ProductId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("productid");

                    b.Property<bool>("Exist")
                        .HasColumnType("boolean")
                        .HasColumnName("exist");

                    b.Property<bool>("Legacy")
                        .HasColumnType("boolean")
                        .HasColumnName("legacy");

                    b.Property<int>("Price")
                        .HasColumnType("integer")
                        .HasColumnName("price");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.Property<long>("RegisteredBlockIndex")
                        .HasColumnType("bigint")
                        .HasColumnName("registeredblockindex");

                    b.Property<string>("SellerAgentAddress")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("selleragentaddress");

                    b.Property<string>("SellerAvatarAddress")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("selleravataraddress");

                    b.Property<string>("product_type")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("product_type");

                    b.HasKey("ProductId")
                        .HasName("pk_products");

                    b.HasIndex("Exist")
                        .HasDatabaseName("ix_products_exist");

                    b.ToTable("products", (string)null);

                    b.HasDiscriminator<string>("product_type").HasValue("ProductModel");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("MarketService.Models.FungibleAssetValueProductModel", b =>
                {
                    b.HasBaseType("MarketService.Models.ProductModel");

                    b.Property<byte>("DecimalPlaces")
                        .HasColumnType("smallint")
                        .HasColumnName("decimalplaces");

                    b.Property<string>("Ticker")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("ticker");

                    b.HasDiscriminator().HasValue("fav");
                });

            modelBuilder.Entity("MarketService.Models.ItemProductModel", b =>
                {
                    b.HasBaseType("MarketService.Models.ProductModel");

                    b.Property<int>("CombatPoint")
                        .HasColumnType("integer")
                        .HasColumnName("combatpoint");

                    b.Property<int>("Crystal")
                        .HasColumnType("integer")
                        .HasColumnName("crystal");

                    b.Property<int>("CrystalPerPrice")
                        .HasColumnType("integer")
                        .HasColumnName("crystalperprice");

                    b.Property<int>("ElementalType")
                        .HasColumnType("integer")
                        .HasColumnName("elementaltype");

                    b.Property<int>("Grade")
                        .HasColumnType("integer")
                        .HasColumnName("grade");

                    b.Property<int>("ItemId")
                        .HasColumnType("integer")
                        .HasColumnName("itemid");

                    b.Property<int>("ItemSubType")
                        .HasColumnType("integer")
                        .HasColumnName("itemsubtype");

                    b.Property<int>("ItemType")
                        .HasColumnType("integer")
                        .HasColumnName("itemtype");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<int>("SetId")
                        .HasColumnType("integer")
                        .HasColumnName("setid");

                    b.Property<Guid>("TradableId")
                        .HasColumnType("uuid")
                        .HasColumnName("tradableid");

                    b.HasDiscriminator().HasValue("item");
                });

            modelBuilder.Entity("MarketService.Models.ItemProductModel", b =>
                {
                    b.OwnsMany("MarketService.Models.SkillModel", "Skills", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer")
                                .HasColumnName("id");

                            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b1.Property<int>("Id"));

                            b1.Property<int>("Chance")
                                .HasColumnType("integer")
                                .HasColumnName("chance");

                            b1.Property<int>("Cooldown")
                                .HasColumnType("integer")
                                .HasColumnName("cooldown");

                            b1.Property<int>("ElementalType")
                                .HasColumnType("integer")
                                .HasColumnName("elementaltype");

                            b1.Property<int>("HitCount")
                                .HasColumnType("integer")
                                .HasColumnName("hitcount");

                            b1.Property<int>("Power")
                                .HasColumnType("integer")
                                .HasColumnName("power");

                            b1.Property<Guid>("ProductId")
                                .HasColumnType("uuid")
                                .HasColumnName("productid");

                            b1.Property<int>("SkillCategory")
                                .HasColumnType("integer")
                                .HasColumnName("skillcategory");

                            b1.Property<int>("SkillId")
                                .HasColumnType("integer")
                                .HasColumnName("skillid");

                            b1.HasKey("Id")
                                .HasName("pk_skillmodel");

                            b1.HasIndex("ProductId")
                                .HasDatabaseName("ix_skillmodel_productid");

                            b1.ToTable("skillmodel", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("ProductId")
                                .HasConstraintName("fk_skillmodel_products_itemproductmodelproductid");
                        });

                    b.OwnsMany("MarketService.Models.StatModel", "Stats", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("integer")
                                .HasColumnName("id");

                            NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b1.Property<int>("Id"));

                            b1.Property<bool>("Additional")
                                .HasColumnType("boolean")
                                .HasColumnName("additional");

                            b1.Property<Guid>("ProductId")
                                .HasColumnType("uuid")
                                .HasColumnName("productid");

                            b1.Property<int>("Type")
                                .HasColumnType("integer")
                                .HasColumnName("type");

                            b1.Property<int>("Value")
                                .HasColumnType("integer")
                                .HasColumnName("value");

                            b1.HasKey("Id")
                                .HasName("pk_statmodel");

                            b1.HasIndex("ProductId")
                                .HasDatabaseName("ix_statmodel_productid");

                            b1.ToTable("statmodel", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("ProductId")
                                .HasConstraintName("fk_statmodel_products_itemproductmodelproductid");
                        });

                    b.Navigation("Skills");

                    b.Navigation("Stats");
                });
#pragma warning restore 612, 618
        }
    }
}
