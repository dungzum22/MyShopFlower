using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyShop.Entities;

namespace MyShop.DataContext;

public partial class FlowershopContext : DbContext
{
    public FlowershopContext()
    {
    }

    public FlowershopContext(DbContextOptions<FlowershopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<FlowerInfo> FlowerInfos { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrdersDetail> OrdersDetails { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<ShopBrand> ShopBrands { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserInfo> UserInfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);Database= Flowershop;UID=sa;PWD=123;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Address");

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.UserInfoId)
                .ValueGeneratedOnAdd()
                .HasColumnName("user_info_id");

            entity.HasOne(d => d.UserInfo).WithMany()
                .HasForeignKey(d => d.UserInfoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Address__descrip__70DDC3D8");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__2EF52A27C309D75A");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.FlowerInfoId).HasColumnName("flower_info_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.FlowerInfo).WithMany(p => p.Carts)
                .HasForeignKey(d => d.FlowerInfoId)
                .HasConstraintName("FK__Cart__flower_inf__4BAC3F29");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Cart__user_id__4AB81AF0");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__D54EE9B46302212F");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(255)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<FlowerInfo>(entity =>
        {
            entity.HasKey(e => e.FlowerId).HasName("PK__Flower_I__177E0A7E5C3B5C58");

            entity.ToTable("Flower_Info", tb => tb.HasTrigger("trg_New_Product_Notification"));

            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.AvailableQuantity).HasColumnName("available_quantity");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FlowerDescription)
                .HasMaxLength(255)
                .HasColumnName("flower_description");
            entity.Property(e => e.FlowerName)
                .HasMaxLength(255)
                .HasColumnName("flower_name");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");

            entity.HasOne(d => d.Category).WithMany(p => p.FlowerInfos)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Flower_In__categ__47DBAE45");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842FF80B6FFC");

            entity.ToTable("Notification");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Message)
                .HasMaxLength(255)
                .HasColumnName("message");
            entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Recipient).WithMany(p => p.NotificationRecipients)
                .HasForeignKey(d => d.RecipientId)
                .HasConstraintName("FK__Notificat__recip__5BE2A6F2");

            entity.HasOne(d => d.Sender).WithMany(p => p.NotificationSenders)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("FK__Notificat__sende__5AEE82B9");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__46596229CE792C5C");

            entity.ToTable(tb => tb.HasTrigger("trg_Order_Success_Notification"));

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(255)
                .HasColumnName("delivery_method");
            entity.Property(e => e.FlowerName)
                .HasMaxLength(255)
                .HasColumnName("flower_name");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(255)
                .HasColumnName("payment_method");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(255)
                .HasColumnName("shipping_address");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Orders__user_id__5070F446");
        });

        modelBuilder.Entity<OrdersDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__Orders_D__3C5A4080D4FAF489");

            entity.ToTable("Orders_Detail");

            entity.Property(e => e.OrderDetailId).HasColumnName("order_detail_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.FlowerName)
                .HasMaxLength(255)
                .HasColumnName("flower_name");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Voucher).HasColumnName("voucher");

            entity.HasOne(d => d.Flower).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.FlowerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Orders_De__flowe__5629CD9C");

            entity.HasOne(d => d.Order).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Orders_De__vouch__5535A963");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Report__779B7C58F309FE13");

            entity.ToTable("Report");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.ReportDescription)
                .HasMaxLength(255)
                .HasColumnName("report_description");
            entity.Property(e => e.ReportReason)
                .HasMaxLength(255)
                .HasColumnName("report_reason");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Flower).WithMany(p => p.Reports)
                .HasForeignKey(d => d.FlowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__flower_i__6D0D32F4");

            entity.HasOne(d => d.Seller).WithMany(p => p.Reports)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__seller_i__6E01572D");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__user_id__6C190EBB");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Request__18D3B90F8177D89B");

            entity.ToTable("Request");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.UserInfoId).HasColumnName("user_info_id");

            entity.HasOne(d => d.Seller).WithMany(p => p.Requests)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Request__seller___74AE54BC");

            entity.HasOne(d => d.UserInfo).WithMany(p => p.Requests)
                .HasForeignKey(d => d.UserInfoId)
                .HasConstraintName("FK__Request__descrip__73BA3083");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Seller__780A0A9799B3DD6B");

            entity.ToTable("Seller");

            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.BusinessName)
                .HasMaxLength(255)
                .HasColumnName("business_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.SellerType)
                .HasMaxLength(20)
                .HasColumnName("seller_type");
            entity.Property(e => e.TaxId)
                .HasMaxLength(50)
                .HasColumnName("tax_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Sellers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Seller__user_id__60A75C0F");
        });

        modelBuilder.Entity<ShopBrand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PK__Shop_Bra__5E5A8E2751B73383");

            entity.ToTable("Shop_Brand");

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.BrandDescription)
                .HasMaxLength(255)
                .HasColumnName("brand_description");
            entity.Property(e => e.BrandName)
                .HasMaxLength(255)
                .HasColumnName("brand_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Seller).WithMany(p => p.ShopBrands)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Shop_Bran__quant__66603565");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FEA4C0C53");

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572532813C2").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.HasKey(e => e.UserInfoId).HasName("PK__User_Inf__82ABEB5408D1EF05");

            entity.ToTable("User_Info");

            entity.Property(e => e.UserInfoId).HasColumnName("user_info_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .HasColumnName("avatar");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsSeller)
                .HasDefaultValue(false)
                .HasColumnName("is_seller");
            entity.Property(e => e.Sex)
                .HasMaxLength(10)
                .HasColumnName("sex");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserInfos)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__User_Info__user___412EB0B6");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
