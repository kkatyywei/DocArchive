//using Microsoft.EntityFrameworkCore;
//using UDocStoreApp.Models;

//namespace UDocStoreApp.Data
//{
//    public class ArchiveDbContext : DbContext
//    {
//        // 1. Добавляем пустой конструктор (теперь он будет использовать OnConfiguring)
//        public ArchiveDbContext() { }

//        public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options) : base(options) { }

//        public DbSet<User> Users { get; set; }
//        public DbSet<Right> Rights { get; set; }
//        public DbSet<Executor> Executors { get; set; }
//        public DbSet<Section> Sections { get; set; }
//        public DbSet<Catalog> Catalogs { get; set; }
//        public DbSet<Order> Orders { get; set; }
//        public DbSet<OrderExecutor> OrderExecutors { get; set; }
//        public DbSet<FileEntity> Files { get; set; }
//        public DbSet<Setting> Settings { get; set; }
//        public DbSet<UserParam> UserParams { get; set; }
//        public DbSet<PassParam> PassParams { get; set; }
//        public DbSet<UsedPassword> UsedPasswords { get; set; }

//        // 2. МЕТОД ПОДКЛЮЧЕНИЯ К БД
//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//                // Server=.; - локальный сервер SQL Server
//                // Database=UDocStoreAppDB; - Имя базы (создай её в SSMS или она создастся сама)
//                // Trusted_Connection=True; - Авторизация через Windows
//                // TrustServerCertificate=True; - Для работы с новыми версиями драйверов
//                optionsBuilder.UseSqlServer(@"Data Source=192.168.5.10;Initial Catalog=DocArchive; User ID = sa; Password=!1qazxcv; TrustServerCertificate=True;");
//            }
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            // Настройка уникальности логина
//            modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique();

//            // Удаление документов каскадно удаляет связи с исполнителями и файлы
//            modelBuilder.Entity<OrderExecutor>()
//                .HasOne(oe => oe.Order)
//                .WithMany(o => o.OrderExecutors)
//                .HasForeignKey(oe => oe.idOrder)
//                .OnDelete(DeleteBehavior.Cascade);

//            modelBuilder.Entity<FileEntity>()
//                .HasOne(f => f.Order)
//                .WithMany(o => o.Files)
//                .HasForeignKey(f => f.idOrder)
//                .OnDelete(DeleteBehavior.Cascade);

//            // Обработка блокировки (idUserOpen) - не каскадно
//            modelBuilder.Entity<Order>()
//                .HasOne(o => o.UserOpen)
//                .WithMany()
//                .HasForeignKey(o => o.idUserOpen)
//                .OnDelete(DeleteBehavior.Restrict);

//            base.OnModelCreating(modelBuilder);
//        }
//    }
//}