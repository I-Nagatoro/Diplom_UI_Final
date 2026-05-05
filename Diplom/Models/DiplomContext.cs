using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Diplom.Models;

public partial class DiplomContext : DbContext
{
    public DiplomContext()
    {
    }

    public DiplomContext(DbContextOptions<DiplomContext> options)
        : base(options)
    {
    }

    public virtual DbSet<History> Histories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=admin;Password=123; Database=diplom;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<History>(entity =>
        {
            entity.HasKey(e => e.HistoryRecordId).HasName("history1_pk");

            entity.ToTable("history");

            entity.Property(e => e.HistoryRecordId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("history_record_id");
            entity.Property(e => e.DatetimeFinish)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datetime_finish");
            entity.Property(e => e.DatetimeOrder)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datetime_order");
            entity.Property(e => e.FileId)
                .HasMaxLength(255)
                .HasColumnName("file_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VideoPath)
                .HasColumnType("character varying")
                .HasColumnName("video_path");
            entity.Property(e => e.VideoUri)
                .HasColumnType("character varying")
                .HasColumnName("video_uri");

            entity.HasOne(d => d.User).WithMany(p => p.Histories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("history_users_fk");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("orders_pk");

            entity.ToTable("orders");

            entity.Property(e => e.OrderId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("order_id");
            entity.Property(e => e.Completed).HasColumnName("completed");
            entity.Property(e => e.DatetimeOrder)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datetime_order");
            entity.Property(e => e.FileId)
                .HasMaxLength(255)
                .HasColumnName("file_id");
            entity.Property(e => e.Progress)
                .HasDefaultValue(0)
                .HasColumnName("progress");
            entity.Property(e => e.ResultPath)
                .HasMaxLength(500)
                .HasColumnName("result_path");
            entity.Property(e => e.Stage)
                .HasDefaultValueSql("''::character varying")
                .HasColumnType("character varying")
                .HasColumnName("stage");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'processing'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.TaskId)
                .HasMaxLength(255)
                .HasColumnName("task_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VideoPath)
                .HasColumnType("character varying")
                .HasColumnName("video_path");
            entity.Property(e => e.VideoUri)
                .HasColumnType("character varying")
                .HasColumnName("video_uri");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("orders_users_fk");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pk");

            entity.ToTable("roles");

            entity.Property(e => e.RoleId)
                .ValueGeneratedNever()
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasColumnType("character varying")
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pk");

            entity.ToTable("users");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.ImagePath)
                .HasColumnType("character varying")
                .HasColumnName("image_path");
            entity.Property(e => e.Login)
                .HasColumnType("character varying")
                .HasColumnName("login");
            entity.Property(e => e.Password)
                .HasColumnType("character varying")
                .HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserName)
                .HasColumnType("character varying")
                .HasColumnName("user_name");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("users_roles_fk");
        });
        modelBuilder.HasSequence("history_history_record_id_seq").HasMax(2147483647L);
        modelBuilder.HasSequence("orders_order_id_seq").HasMax(2147483647L);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
