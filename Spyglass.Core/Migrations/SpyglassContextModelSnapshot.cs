﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Spyglass.Core.Database;

#nullable disable

namespace Spyglass.Core.Migrations
{
    [DbContext(typeof(SpyglassContext))]
    partial class SpyglassContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.HasSequence<int>("PlayerSanctionIds");

            modelBuilder.Entity("Spyglass.Models.Admin.AuthenticatedRequestData", b =>
                {
                    b.Property<string>("ClientId")
                        .HasColumnType("text")
                        .HasColumnName("client_id");

                    b.Property<string>("IpAddress")
                        .HasColumnType("text")
                        .HasColumnName("ip_address");

                    b.Property<DateTimeOffset>("RequestTime")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("request_time")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("ServerName")
                        .HasColumnType("text")
                        .HasColumnName("server_name");

                    b.HasKey("ClientId", "IpAddress")
                        .HasName("pk_authenticated_requests");

                    b.ToTable("authenticated_requests", (string)null);
                });

            modelBuilder.Entity("Spyglass.Models.Admin.MaintainerIdentity", b =>
                {
                    b.Property<string>("UniqueId")
                        .HasColumnType("text")
                        .HasColumnName("unique_id");

                    b.Property<string>("ClientId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("client_id");

                    b.HasKey("UniqueId")
                        .HasName("pk_maintainer_identities");

                    b.ToTable("maintainer_identities", (string)null);
                });

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
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("is_maintainer");

                    b.Property<DateTimeOffset>("LastSeenAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_seen_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("LastSeenOnServer")
                        .HasColumnType("text")
                        .HasColumnName("last_seen_on_server");

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

                    b.Property<string>("IssuerId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("issuer_id");

                    b.Property<int>("PunishmentType")
                        .HasColumnType("integer")
                        .HasColumnName("punishment_type");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<string>("UniqueId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("unique_id");

                    b.HasKey("Id")
                        .HasName("pk_sanctions");

                    b.HasIndex("IssuerId")
                        .HasDatabaseName("ix_sanctions_issuer_id");

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
                    b.HasOne("Spyglass.Models.PlayerInfo", "IssuerInfo")
                        .WithMany()
                        .HasForeignKey("IssuerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_sanctions_players_issuer_info_unique_id");

                    b.HasOne("Spyglass.Models.PlayerInfo", "OwningPlayer")
                        .WithMany("Sanctions")
                        .HasForeignKey("UniqueId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_sanctions_players_owning_player_unique_id");

                    b.Navigation("IssuerInfo");

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
