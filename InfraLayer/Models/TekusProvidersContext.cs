using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace InfraLayer.Models;

public partial class TekusProvidersContext : DbContext
{
    public TekusProvidersContext()
    {
    }

    public TekusProvidersContext(DbContextOptions<TekusProvidersContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Countries> Countries { get; set; }

    public virtual DbSet<CustomFields> CustomFields { get; set; }

    public virtual DbSet<Providers> Providers { get; set; }

    public virtual DbSet<ProvidersServices> ProvidersServices { get; set; }

    public virtual DbSet<Services> Services { get; set; }

    public virtual DbSet<ServicesCountries> ServicesCountries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Countries>(entity =>
        {
            entity.Property(e => e.FlagImage).HasColumnType("text");
            entity.Property(e => e.Isocode)
                .HasMaxLength(10)
                .HasColumnName("ISOCode");
            entity.Property(e => e.Name).HasColumnType("text");
        });

        modelBuilder.Entity<CustomFields>(entity =>
        {
            entity.Property(e => e.FieldName).HasColumnType("text");
            entity.Property(e => e.FieldValue).HasColumnType("text");

            entity.HasOne(d => d.IdProviderNavigation).WithMany(p => p.CustomFields)
                .HasForeignKey(d => d.IdProvider)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomFields_Providers");
        });

        modelBuilder.Entity<Providers>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nit)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NIT");
        });

        modelBuilder.Entity<ProvidersServices>(entity =>
        {
            entity.HasOne(d => d.IdProviderNavigation).WithMany(p => p.ProvidersServices)
                .HasForeignKey(d => d.IdProvider)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProvidersServices_Providers");

            entity.HasOne(d => d.IdServiceNavigation).WithMany(p => p.ProvidersServices)
                .HasForeignKey(d => d.IdService)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProvidersServices_Services");
        });

        modelBuilder.Entity<Services>(entity =>
        {
            entity.Property(e => e.Name).HasColumnType("text");
            entity.Property(e => e.ValuePerHourUsd)
                .HasColumnType("text")
                .HasColumnName("ValuePerHourUSD");
        });

        modelBuilder.Entity<ServicesCountries>(entity =>
        {
            entity.HasOne(d => d.IdCountryNavigation).WithMany(p => p.ServicesCountries)
                .HasForeignKey(d => d.IdCountry)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicesCountries_Countries1");

            entity.HasOne(d => d.IdServiceNavigation).WithMany(p => p.ServicesCountries)
                .HasForeignKey(d => d.IdService)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServicesCountries_Countries");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
