using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Models;

namespace UDocStoreApp.Data
{
    public class ArchiveDbContext : DbContext
    {
        public ArchiveDbContext() { }

        public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : base(options) { }

        // ТАБЛИЦЫ БАЗЫ ДАННЫХ
        public DbSet<User> Users { get; set; }
        public DbSet<Right> Rights { get; set; }
        public DbSet<Executor> Executors { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderExecutor> OrderExecutors { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<OrderFile> OrderFiles { get; set; } // Таблица связей Многие-ко-Многим для файлов
        public DbSet<Setting> Settings { get; set; }
        public DbSet<UserParam> UserParams { get; set; }
        public DbSet<PassParam> PassParams { get; set; }
        public DbSet<UsedPassword> UsedPasswords { get; set; }

        // НАСТРОЙКА ПОДКЛЮЧЕНИЯ
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Database=DocArchive - имя твоей базы данных
                optionsBuilder.UseSqlServer(@"Data Source=192.168.5.10;Initial Catalog=DocArchive; User ID = sa; Password=!1qazxcv; TrustServerCertificate=True;");
            }
        }

        // НАСТРОЙКА СВЯЗЕЙ (Fluent API)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Уникальный логин
            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();

            // Каскадное удаление для связей документов с исполнителями
            modelBuilder.Entity<OrderExecutor>()
                .HasOne(oe => oe.Order)
                .WithMany(o => o.OrderExecutors)
                .HasForeignKey(oe => oe.idOrder)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь файлов (Многие-ко-Многим)
            modelBuilder.Entity<OrderFile>()
                .HasOne(of => of.Order)
                .WithMany() // У документа много связей с файлами
                .HasForeignKey(of => of.idOrder)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderFile>()
                .HasOne(of => of.File)
                .WithMany() // У файла много связей с документами
                .HasForeignKey(of => of.idFile)
                .OnDelete(DeleteBehavior.Cascade);

            // Блокировка документа (idUserOpen) - без каскадного удаления
            modelBuilder.Entity<Order>()
                .HasOne(o => o.UserOpen)
                .WithMany()
                .HasForeignKey(o => o.idUserOpen)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}