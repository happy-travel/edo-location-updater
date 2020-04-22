﻿using HappyTravel.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Data
{
    public class LocationUpdaterContext : DbContext
    {
        public LocationUpdaterContext(DbContextOptions<LocationUpdaterContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis");
            BuildLocation(builder);
        }


        private void BuildLocation(ModelBuilder builder)
        {
            builder.Entity<Location>(typeBuilder =>
            {
                typeBuilder.ToTable("Locations");
                typeBuilder.HasKey(l => l.Id);
                typeBuilder.Property(l => l.Id).ValueGeneratedNever();
                typeBuilder.Property(l => l.Coordinates).HasColumnType("geography (point)");
                typeBuilder.Property(l => l.Name).HasColumnType("jsonb");
                typeBuilder.Property(l => l.Locality).HasColumnType("jsonb");
                typeBuilder.Property(l => l.Country).HasColumnType("jsonb");
                typeBuilder.Property(l => l.Distance);
                typeBuilder.Property(l => l.Source);
                typeBuilder.Property(l => l.Type);
                typeBuilder.Property(l => l.DataProviders).HasColumnType("jsonb");
            });
        }
        
        
        public DbSet<Location> Locations { get; set; }
    }
}