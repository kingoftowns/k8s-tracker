﻿// <auto-generated />
using System;
using System.Collections.Generic;
using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KubernetesTracker.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Cluster", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiserverVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ClusterName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("KernelVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("KubeletVersion")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ClusterName")
                        .IsUnique();

                    b.ToTable("Clusters");
                });

            modelBuilder.Entity("Ingress", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ClusterId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.PrimitiveCollection<List<string>>("Hosts")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("IngressName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Namespace")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ClusterId", "Namespace", "IngressName")
                        .IsUnique();

                    b.ToTable("Ingresses");
                });

            modelBuilder.Entity("Service", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ClusterId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ExternalIp")
                        .HasColumnType("text");

                    b.Property<string>("Namespace")
                        .IsRequired()
                        .HasColumnType("text");

                    b.PrimitiveCollection<List<int>>("Ports")
                        .IsRequired()
                        .HasColumnType("integer[]");

                    b.Property<string>("ServiceName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ClusterId", "Namespace", "ServiceName")
                        .IsUnique();

                    b.ToTable("Services");
                });

            modelBuilder.Entity("Ingress", b =>
                {
                    b.HasOne("Cluster", "Cluster")
                        .WithMany("Ingresses")
                        .HasForeignKey("ClusterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Cluster");
                });

            modelBuilder.Entity("Service", b =>
                {
                    b.HasOne("Cluster", "Cluster")
                        .WithMany("Services")
                        .HasForeignKey("ClusterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Cluster");
                });

            modelBuilder.Entity("Cluster", b =>
                {
                    b.Navigation("Ingresses");

                    b.Navigation("Services");
                });
#pragma warning restore 612, 618
        }
    }
}
