﻿using Microsoft.EntityFrameworkCore;
using Spyglass.Models;

namespace Spyglass.Core.Database
{
    public class SpyglassContext : DbContext
    {
        public SpyglassContext(DbContextOptions<SpyglassContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Table containing all the tracked Titanfall 2 players with their basic information.
        /// </summary>
        public DbSet<PlayerInfo> Players { get; set; } = null!;

        /// <summary>
        /// Table containing known username aliases for tracked players.
        /// </summary>
        public DbSet<PlayerAlias> PlayerAliases { get; set; } = null!;

        /// <summary>
        /// Table containing all the sanctions issued to players.
        /// </summary>
        public DbSet<PlayerSanction> Sanctions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasSequence<int>("PlayerSanctionIds")
                .StartsAt(1)
                .IncrementsBy(1);

            modelBuilder.Entity<PlayerInfo>(e =>
            {
                e.HasKey(p => p.UniqueID);
                e.Property(p => p.CreatedAt)
                    .HasDefaultValueSql("now()");
                e.Ignore(p => p.KnownAliases);
            });

            modelBuilder.Entity<PlayerAlias>(e =>
            {
                e.HasKey(a => new {a.UniqueID, a.Alias});
                e.HasOne(a => a.OwningPlayer)
                    .WithMany(p => p.Aliases)
                    .HasForeignKey(a => a.UniqueID);
            });

            modelBuilder.Entity<PlayerSanction>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Id)
                    .HasDefaultValueSql("nextval('\"PlayerSanctionIds\"')");

                e.Property(s => s.IssuedAt)
                    .HasDefaultValueSql("now()");

                e.HasOne(s => s.OwningPlayer)
                    .WithMany(p => p.Sanctions)
                    .HasForeignKey(s => s.UniqueId);
                
                e.Ignore(s => s.IssuedAtTimestamp);
                e.Ignore(s => s.IssuedAtReadable);
                e.Ignore(s => s.ExpiresAtTimestamp);
                e.Ignore(s => s.ExpiresAtReadable);
            });
        }
    }
}