using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Models;
using Microsoft.Extensions.Logging;
using System;

namespace UDocStoreApp.Data
{
    public class ArchiveDbContext : DbContext
    {
        public ArchiveDbContext() { }

        public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Right> Rights { get; set; }
        public DbSet<Executor> Executors { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderExecutor> OrderExecutors { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<OrderFile> OrderFiles { get; set; } 
        public DbSet<PassParam> PassParams { get; set; }
        public DbSet<UsedPassword> UsedPasswords { get; set; }

   

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=192.168.5.10;Initial Catalog=DocArchive; User ID = sa; Password=!1qazxcv; TrustServerCertificate=True; Encrypt=False")
                                        .LogTo(Console.WriteLine, LogLevel.Information); 

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();

            modelBuilder.Entity<OrderExecutor>()
                .HasOne(oe => oe.Order)
                .WithMany(o => o.OrderExecutors)
                .HasForeignKey(oe => oe.idOrder)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderFile>(entity =>
            {
                entity.ToTable("OrderFiles");
                entity.HasKey(e => e.id);

                entity.HasOne(of => of.Order)
                    .WithMany(o => o.OrderFiles) 
                    .HasForeignKey(of => of.idOrder);

                entity.HasOne(of => of.File)
                    .WithMany() 
                    .HasForeignKey(of => of.idFile);
            });

            modelBuilder.Entity<FileEntity>(entity =>
            {
                entity.ToTable("Files");
                entity.HasKey(e => e.id);
            });

            modelBuilder.Entity<FileEntity>().ToTable("Files");
            modelBuilder.Entity<Order>()
               .HasOne(o => o.UserOpen)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);
            base.OnModelCreating(modelBuilder);
        }
    }
}