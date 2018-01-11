using System;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BLL.DBModels
{
    public partial class AutoQuestraderContext : DbContext
    {
        public virtual DbSet<AccountCategory> AccountCategory { get; set; }
        public virtual DbSet<Category> Category { get; set; }
        public virtual DbSet<SettingValues> SettingValues { get; set; }
        public virtual DbSet<StockTarget> StockTarget { get; set; }
        public virtual DbSet<Token> Token { get; set; }

        public AutoQuestraderContext(DbContextOptions<AutoQuestraderContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
               // optionsBuilder.UseSqlServer(System.Configuration.GetConnectionString("AutoQuestraderDatabase"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountCategory>(entity =>
            {
                entity.HasKey(e => new { e.AccountNumber, e.CategoryName });

                entity.Property(e => e.AccountNumber)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CategoryName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.CategoryNameNavigation)
                    .WithMany(p => p.AccountCategory)
                    .HasForeignKey(d => d.CategoryName)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_AccountCategory_Category");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<SettingValues>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .ValueGeneratedNever();

                entity.Property(e => e.Value).IsUnicode(false);
            });

            modelBuilder.Entity<StockTarget>(entity =>
            {
                entity.HasKey(e => new { e.Symbol, e.CategoryName });

                entity.Property(e => e.Symbol)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CategoryName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.CategoryNameNavigation)
                    .WithMany(p => p.StockTarget)
                    .HasForeignKey(d => d.CategoryName)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_StockTarget_Category");
            });

            modelBuilder.Entity<Token>(entity =>
            {
                entity.HasKey(e => e.LoginServer);

                entity.Property(e => e.LoginServer)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.AccessToken)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ApiServer)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RefreshToken)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TokenType)
                    .HasMaxLength(100)
                    .IsUnicode(false);
            });
        }
    }
}
