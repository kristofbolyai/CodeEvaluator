﻿// <auto-generated />
using System;
using CodeEvaluator.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CodeEvaluator.Data.Migrations
{
    [DbContext(typeof(CodeDataDbContext))]
    [Migration("20240327064545_FileNameIsNotRequired")]
    partial class FileNameIsNotRequired
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("CodeEvaluator.Data.Models.CodeSubmission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("TEXT")
                        .HasComment("The date and time when the code execution was completed. This field is both set on successful and failed code execution.");

                    b.Property<int>("Language")
                        .HasColumnType("INTEGER")
                        .HasComment("The language of the code to be executed.");

                    b.Property<DateTime>("QueuedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("StartedAt")
                        .HasColumnType("TEXT")
                        .HasComment("The date and time when the code execution was started.");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("CodeSubmissions");
                });
#pragma warning restore 612, 618
        }
    }
}