﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wism.Client.Data.DbContexts;

namespace Wism.Client.Data.Migrations
{
    [DbContext(typeof(WismClientDbContext))]
    [Migration("20201013162445_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8");

            modelBuilder.Entity("Wism.Client.Data.Entities.Army", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("HitPoints")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Strength")
                        .HasColumnType("INTEGER");

                    b.Property<int>("X")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Armies");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            HitPoints = 2,
                            Name = "Hero",
                            Strength = 5,
                            X = 0,
                            Y = 0
                        },
                        new
                        {
                            Id = 2,
                            HitPoints = 2,
                            Name = "Light Infantry",
                            Strength = 3,
                            X = 0,
                            Y = 5
                        });
                });

            modelBuilder.Entity("Wism.Client.Data.Entities.Command", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Commands");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Command");
                });

            modelBuilder.Entity("Wism.Client.Data.Entities.ArmyAttackCommand", b =>
                {
                    b.HasBaseType("Wism.Client.Data.Entities.Command");

                    b.Property<int>("X")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("ArmyAttackCommand");
                });

            modelBuilder.Entity("Wism.Client.Data.Entities.ArmyMoveCommand", b =>
                {
                    b.HasBaseType("Wism.Client.Data.Entities.Command");

                    b.Property<int>("X")
                        .HasColumnName("ArmyMoveCommand_X")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Y")
                        .HasColumnName("ArmyMoveCommand_Y")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("ArmyMoveCommand");
                });
#pragma warning restore 612, 618
        }
    }
}