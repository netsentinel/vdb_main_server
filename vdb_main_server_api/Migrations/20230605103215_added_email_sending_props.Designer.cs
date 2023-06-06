﻿// <auto-generated />
using System;
using System.Collections.Generic;
using DataAccessLayer.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace main_server_api.Migrations
{
    [DbContext(typeof(VpnContext))]
    [Migration("20230605103215_added_email_sending_props")]
    partial class added_email_sending_props
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DataAccessLayer.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsEmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastSendedEmail")
                        .HasColumnType("timestamp with time zone");

                    b.Property<byte[]>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("bytea");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("bytea");

                    b.Property<DateTime>("PayedUntil")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("RecoveryJwtEntropy")
                        .HasColumnType("bigint");

                    b.Property<List<long>>("RefreshTokensEntropies")
                        .IsRequired()
                        .HasColumnType("bigint[]");

                    b.HasKey("Id");

                    b.HasAlternateKey("Email");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DataAccessLayer.Models.UserDevice", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<int?>("LastConnectedNodeId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("LastSeenUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<string>("WireguardPublicKey")
                        .IsRequired()
                        .HasMaxLength(45)
                        .HasColumnType("character varying(45)");

                    b.HasKey("Id");

                    b.HasAlternateKey("WireguardPublicKey");

                    b.ToTable("Devices");
                });
#pragma warning restore 612, 618
        }
    }
}
