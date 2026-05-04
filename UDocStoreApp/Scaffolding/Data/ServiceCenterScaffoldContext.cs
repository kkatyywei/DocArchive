using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UDocStoreApp.Scaffolding.Models;

namespace UDocStoreApp.Scaffolding.Data;

public partial class ServiceCenterScaffoldContext : DbContext
{
    public ServiceCenterScaffoldContext()
    {
    }

    public ServiceCenterScaffoldContext(DbContextOptions<ServiceCenterScaffoldContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Catalog> Catalogs { get; set; }

    public virtual DbSet<Executor> Executors { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderExecutor> OrderExecutors { get; set; }

    public virtual DbSet<PassParam> PassParams { get; set; }

    public virtual DbSet<Right> Rights { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<UsedPassword> UsedPasswords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserParam> UserParams { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=192.168.5.10;Initial Catalog=DocArchive;User ID=sa;Password=!1qazxcv;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Catalog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Catalog__3213E83F9B74609B");

            entity.ToTable("Catalog");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CatalogName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IdSection).HasColumnName("idSection");
            entity.Property(e => e.NumberNext).HasDefaultValue(1);

            entity.HasOne(d => d.IdSectionNavigation).WithMany(p => p.Catalogs)
                .HasForeignKey(d => d.IdSection)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Catalog_Section");
        });

        modelBuilder.Entity<Executor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Executor__3213E83F7BC699E5");

            entity.ToTable("Executor");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(1);
            entity.Property(e => e.Dol)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fio)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("FIO");
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Files__3213E83FA642BDDF");

            entity.HasIndex(e => e.IdOrder, "IX_Files_idOrder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.IdOrder).HasColumnName("idOrder");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.IdOrderNavigation).WithMany(p => p.Files)
                .HasForeignKey(d => d.IdOrder)
                .HasConstraintName("FK_Files_Order");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3213E83F11BBB603");

            entity.ToTable("Order");

            entity.HasIndex(e => e.NumberOrder, "IX_Order_NumberOrder");

            entity.HasIndex(e => e.IsDel, "IX_Order_isDel");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateOrder).HasColumnType("datetime");
            entity.Property(e => e.IdCatalog).HasColumnName("idCatalog");
            entity.Property(e => e.IdUser).HasColumnName("idUser");
            entity.Property(e => e.IdUserOpen).HasColumnName("idUserOpen");
            entity.Property(e => e.IsDel).HasColumnName("isDel");
            entity.Property(e => e.NumberOrder)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RegDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Text)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.IdCatalogNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.IdCatalog)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_Catalog");

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.OrderIdUserNavigations)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_User_Author");

            entity.HasOne(d => d.IdUserOpenNavigation).WithMany(p => p.OrderIdUserOpenNavigations)
                .HasForeignKey(d => d.IdUserOpen)
                .HasConstraintName("FK_Order_User_Lock");
        });

        modelBuilder.Entity<OrderExecutor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderExe__3213E83F15F9CB69");

            entity.ToTable("OrderExecutor");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdExecutor).HasColumnName("idExecutor");
            entity.Property(e => e.IdOrder).HasColumnName("idOrder");

            entity.HasOne(d => d.IdExecutorNavigation).WithMany(p => p.OrderExecutors)
                .HasForeignKey(d => d.IdExecutor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderExecutor_Executor");

            entity.HasOne(d => d.IdOrderNavigation).WithMany(p => p.OrderExecutors)
                .HasForeignKey(d => d.IdOrder)
                .HasConstraintName("FK_OrderExecutor_Order");
        });

        modelBuilder.Entity<PassParam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PassPara__3213E83F6AA85649");

            entity.ToTable("PassParam");

            entity.Property(e => e.Id)
                .HasDefaultValue(1)
                .HasColumnName("id");
            entity.Property(e => e.CountLast).HasDefaultValue(3);
            entity.Property(e => e.MaxPeriod).HasDefaultValue(90);
            entity.Property(e => e.MinWidth).HasDefaultValue(6);
            entity.Property(e => e.MinWidthCheck).HasDefaultValue(true);
        });

        modelBuilder.Entity<Right>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rights__3213E83F02F321CC");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Section__3213E83F87BE75FC");

            entity.ToTable("Section");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SectionName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("PK__Settings__737584F702F584D1");

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UsedPassword>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UsedPass__3213E83FB3F2C922");

            entity.ToTable("UsedPassword");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdUser).HasColumnName("id_User");
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.UsedPasswords)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsedPassword_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F502CFB38");

            entity.ToTable("User");

            entity.HasIndex(e => e.Login, "UQ__User__5E55825BDCF5C07B").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(1);
            entity.Property(e => e.IdExecutor).HasColumnName("idExecutor");
            entity.Property(e => e.IdRights).HasColumnName("idRights");
            entity.Property(e => e.Login)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdExecutorNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.IdExecutor)
                .HasConstraintName("FK_User_Executor");

            entity.HasOne(d => d.IdRightsNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.IdRights)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Rights");
        });

        modelBuilder.Entity<UserParam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserPara__3213E83FF21F38E6");

            entity.ToTable("UserParam");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUser).HasColumnName("idUser");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.IdUserNavigation).WithMany(p => p.UserParams)
                .HasForeignKey(d => d.IdUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserParam_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
