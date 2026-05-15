using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VelvetCakes.Api.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Component> Components { get; set; }

    public virtual DbSet<CustomCake> CustomCakes { get; set; }

    public virtual DbSet<CustomCakeComponent> CustomCakeComponents { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=VelvetCakes;Username=postgres;Password=postgres123");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart__3214EC071BE5D092");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.CustomCake).WithMany(p => p.Carts)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Cart__CustomCake__48CFD27E");

            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Cart__ProductId__47DBAE45");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasConstraintName("FK__Cart__UserId__46E78A0C");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Chats__3214EC07");

            entity.ToTable("Chats");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User)
                .WithMany(u => u.ChatUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Chats__UserId");

            entity.HasOne(d => d.Manager)
                .WithMany(u => u.ChatManagers)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK__Chats__ManagerId");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMessages__3214EC07");

            entity.ToTable("ChatMessages");

            entity.Property(e => e.SenderRole)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Message)
                .IsRequired();

            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(d => d.ChatId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ChatMessages__ChatId");

            entity.HasOne(d => d.Sender)
                .WithMany(u => u.ChatMessages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK__ChatMessages__SenderId");
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Componen__3214EC07237D488F");

            entity.Property(e => e.ComplexityPoints).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsSeasonal).HasDefaultValue(false);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.BasePricePerUnit).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<CustomCake>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CustomCa__3214EC07354A90C0");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasDefaultValue("Индивидуальный торт");
            entity.Property(e => e.Weight).HasColumnType("decimal(4,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");

            entity.HasOne(d => d.User).WithMany(p => p.CustomCakes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__CustomCak__UserI__3B75D760");
        });

        modelBuilder.Entity<CustomCakeComponent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CustomCa__3214EC07DE24AB9E");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Component).WithMany(p => p.CustomCakeComponents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CustomCak__Compo__412EB0B6");

            entity.HasOne(d => d.CustomCake).WithMany(p => p.CustomCakeComponents)
                .HasForeignKey(d => d.CustomCakeId)
                .HasConstraintName("FK__CustomCak__Custo__403A8C7D");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Favorites__3214EC07");

            entity.HasIndex(e => new { e.UserId, e.ProductId }, "UQ_Favorites").IsUnique();

            entity.Property(e => e.AddedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Favorites__ProductId");

            entity.HasOne(d => d.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Favorites__UserId");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07C25882E1");

            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__656C112C");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC071C60A195");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaidAmount).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasDefaultValue("Новый");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.DeliveryAddress).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Orders__UserId__534D60F1");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3214EC07B5701DC7");

            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");

            entity.HasOne(d => d.CustomCake).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Custo__5AEE82B9");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__59063A47");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__OrderItem__Produ__59FA5E80");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC07433655F9");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Weight).HasMaxLength(50);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC072CE4C9D0");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.AuthorName).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Reviews__UserId__60A75C0F");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC07FD704E76");

            entity.HasIndex(e => e.Name, "UQ__Roles__737584F6CADB1687").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0771E1803D");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__29572725");
        });

        modelBuilder.Entity<UserPaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserPaym__3214EC071B749341");

            entity.Property(e => e.CardLast4).HasMaxLength(4);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.MethodName).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserPaymentMethods)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__UserPayme__UserI__70DDC3D8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}