﻿// <auto-generated />
using System;
using ExchangeRates.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExchangeRates.Migrations.Migrations
{
    [DbContext(typeof(DataDb))]
    partial class DataDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.8");

            modelBuilder.Entity("ExchangeRates.Infrastructure.DB.Models.ValuteModelDb", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("CharCode")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Nominal")
                        .HasColumnType("INTEGER");

                    b.Property<string>("NumCode")
                        .HasColumnType("TEXT");

                    b.Property<double>("Previous")
                        .HasColumnType("REAL");

                    b.Property<DateTime>("Time")
                        .HasColumnType("TEXT");

                    b.Property<double>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("Valutes");
                });
#pragma warning restore 612, 618
        }
    }
}
