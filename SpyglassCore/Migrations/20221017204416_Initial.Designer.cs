﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Spyglass.Core.Database;

#nullable disable

namespace Spyglass.Core.Migrations
{
    [DbContext(typeof(SpyglassContext))]
    [Migration("20221017204416_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.HasSequence<int>("PlayerSanctionIds");

            modelBuilder.Entity("Spyglass.Models.PlayerAlias", b =>
                {
                    b.Property<string>("UniqueID")
                        .HasColumnType("text")
                        .HasColumnName("unique_id");

                    b.Property<string>("Alias")
                        .HasColumnType("text")
                        .HasColumnName("alias");

                    b.HasKey("UniqueID", "Alias")
                        .HasName("pk_player_aliases");

                    b.ToTable("player_aliases", (string)null);
                });

            modelBuilder.Entity("Spyglass.Models.PlayerInfo", b =>
                {
                    b.Property<string>("UniqueID")
                        .HasColumnType("text")
                        .HasColumnName("unique_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<bool>("IsMaintainer")
                        .HasColumnType("boolean")
                        .HasColumnName("is_maintainer");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("UniqueID")
                        .HasName("pk_players");

                    b.ToTable("players", (string)null);
                });

            modelBuilder.Entity("Spyglass.Models.PlayerSanction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .HasDefaultValueSql("nextval('\"PlayerSanctionIds\"')");

                    b.Property<DateTimeOffset?>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<DateTimeOffset>("IssuedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("issued_at")
                        .HasDefaultValueSql("now()");

                    b.Property<decimal>("IssuerId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("issuer_id");

                    b.Property<int>("PunishmentType")
                        .HasColumnType("integer")
                        .HasColumnName("punishment_type");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<string>("ReportMessage")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("report_message");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<string>("UniqueId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("unique_id");

                    b.HasKey("Id")
                        .HasName("pk_sanctions");

                    b.HasIndex("UniqueId")
                        .HasDatabaseName("ix_sanctions_unique_id");

                    b.ToTable("sanctions", (string)null);
                });

            modelBuilder.Entity("Spyglass.Models.PlayerAlias", b =>
                {
                    b.HasOne("Spyglass.Models.PlayerInfo", "OwningPlayer")
                        .WithMany("Aliases")
                        .HasForeignKey("UniqueID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_player_aliases_players_owning_player_temp_id");

                    b.Navigation("OwningPlayer");
                });

            modelBuilder.Entity("Spyglass.Models.PlayerSanction", b =>
                {
                    b.HasOne("Spyglass.Models.PlayerInfo", "OwningPlayer")
                        .WithMany("Sanctions")
                        .HasForeignKey("UniqueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_sanctions_players_owning_player_temp_id1");

                    b.Navigation("OwningPlayer");
                });

            modelBuilder.Entity("Spyglass.Models.PlayerInfo", b =>
                {
                    b.Navigation("Aliases");

                    b.Navigation("Sanctions");
                });
#pragma warning restore 612, 618
        }
    }
}
