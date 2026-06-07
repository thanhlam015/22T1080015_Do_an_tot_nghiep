using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class DoAnTotNghiepContext : DbContext
{
    public DoAnTotNghiepContext()
    {
    }

    public DoAnTotNghiepContext(DbContextOptions<DoAnTotNghiepContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accommodation> Accommodations { get; set; }

    public virtual DbSet<AccommodationAmenity> AccommodationAmenities { get; set; }

    public virtual DbSet<AccommodationImage> AccommodationImages { get; set; }

    public virtual DbSet<AccommodationRule> AccommodationRules { get; set; }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Landmark> Landmarks { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PropertyType> PropertyTypes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomAvailabilityPricing> RoomAvailabilityPricings { get; set; }

    public virtual DbSet<SavedAccommodation> SavedAccommodations { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<RoomImage> RoomImages { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionAccommodation> PromotionAccommodations { get; set; }

    public virtual DbSet<RagDocument> RagDocuments { get; set; }

    public virtual DbSet<RagChunk> RagChunks { get; set; }

    public virtual DbSet<BotQuestionLog> BotQuestionLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=LAPTOP-VJKO8QC7;Database=DoAnTotNghiep;User Id=sa;Password=123;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Accommodation>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(50);

            entity.HasOne(d => d.District).WithMany(p => p.Accommodations)
                .HasForeignKey(d => d.DistrictId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accommodations_Districts");

            entity.HasOne(d => d.PropertyType).WithMany(p => p.Accommodations)
                .HasForeignKey(d => d.PropertyTypeId)
                .HasConstraintName("FK_Accommodations_PropertyTypes");
            entity.Property(e => e.AiIndexStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("NotIndexed");

            entity.Property(e => e.AiLastIndexedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.AiIndexError)
                .HasMaxLength(500);
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 7)");

            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(10, 7)");
        });

        modelBuilder.Entity<AccommodationAmenity>(entity =>
        {
            entity.HasKey(e => new { e.AccommodationId, e.AmenityId });

            entity.ToTable("Accommodation_Amenities");

            entity.HasOne(d => d.Accommodation).WithMany(p => p.AccommodationAmenities)
                .HasForeignKey(d => d.AccommodationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accommodation_Amenities_Accommodations");

            entity.HasOne(d => d.Amenity).WithMany(p => p.AccommodationAmenities)
                .HasForeignKey(d => d.AmenityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accommodation_Amenities_Amenities");
        });

        modelBuilder.Entity<AccommodationImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

           // entity.Property(e => e.ImageId).ValueGeneratedNever();
            entity.Property(e => e.ImageUrl).HasMaxLength(255);

            entity.HasOne(d => d.Accommodation).WithMany(p => p.AccommodationImages)
                .HasForeignKey(d => d.AccommodationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AccommodationImages_Accommodations");
        });

        modelBuilder.Entity<AccommodationRule>(entity =>
        {
            entity.HasKey(e => e.AccommodationId).HasName("PK__Accommod__DBB30A5158757E74");

            entity.Property(e => e.AccommodationId).ValueGeneratedNever();
            entity.Property(e => e.AgeRestriction).HasMaxLength(255);
            entity.Property(e => e.CheckInTime)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CheckOutTime)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PetPolicy).HasMaxLength(255);

            entity.HasOne(d => d.Accommodation).WithOne(p => p.AccommodationRule)
                .HasForeignKey<AccommodationRule>(d => d.AccommodationId)
                .HasConstraintName("FK__Accommoda__Accom__55009F39");
        });

        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Hotel");
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CheckInDate)
                .HasColumnType("datetime");

            entity.Property(e => e.CheckOutDate)
                .HasColumnType("datetime");

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.FullName)
                .HasMaxLength(100);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.Status)
                .HasMaxLength(50);

            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18, 0)");

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PayAtHotel");

            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Unpaid");

            entity.Property(e => e.NumberOfRooms)
                .HasDefaultValue(1);

            entity.Property(e => e.AdultCount)
                .HasDefaultValue(1);

            entity.Property(e => e.ChildCount)
                .HasDefaultValue(0);

            entity.Property(e => e.ConfirmedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CancelledAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CancelReason)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.Room)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Rooms1");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Users1");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMess__3214EC079F3AFE41");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SenderRole)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Session).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_ChatMessages_Sessions");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatSess__3214EC0750F72EB3");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastUpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SessionTitle)
                .HasMaxLength(255)
                .HasDefaultValue("Cuộc trò chuyện mới");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Employee__3214EC0741270321");

            entity.HasIndex(e => e.EmployeeCode, "UQ__Employee__1F642548D6239C40").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Employee__A9D1053470F0D8C9").IsUnique();

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Staff");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<Landmark>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Landmark__3214EC074616AFE2");

            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(100);

            entity.HasOne(d => d.District).WithMany(p => p.Landmarks)
                .HasForeignKey(d => d.DistrictId)
                .HasConstraintName("FK__Landmarks__Distr__2739D489");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38C7E126A5");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Payments_Bookings");
        });

        modelBuilder.Entity<PropertyType>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NamePropertyTypes).HasMaxLength(50);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Users");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(e => e.BedType).HasMaxLength(100);
            entity.Property(e => e.PriceNight).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.RoomType).HasMaxLength(50);

            entity.HasOne(d => d.Accommodation).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.AccommodationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Rooms_Accommodations");

            entity.HasMany(d => d.Amenities).WithMany(p => p.Rooms)
                .UsingEntity<Dictionary<string, object>>(
                    "RoomAmenity",
                    r => r.HasOne<Amenity>().WithMany()
                        .HasForeignKey("AmenityId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Room_Amen__Ameni__245D67DE"),
                    l => l.HasOne<Room>().WithMany()
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Room_Amen__RoomI__236943A5"),
                    j =>
                    {
                        j.HasKey("RoomId", "AmenityId").HasName("PK__Room_Ame__9AC49669FF10FD57");
                        j.ToTable("Room_Amenities");
                    });
        });
        modelBuilder.Entity<RoomImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.Property(e => e.ImageId)
                .HasColumnName("ImageId");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255);

            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Room)
                .WithMany(p => p.RoomImages)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_RoomImages_Rooms")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomAvailabilityPricing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoomAvai__3214EC076C4E94F3");

            entity.ToTable("RoomAvailabilityPricing");

            entity.Property(e => e.PricePerNight).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Room).WithMany(p => p.RoomAvailabilityPricings)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__RoomAvail__RoomI__57DD0BE4");
            entity.Property(e => e.TargetDate)
                .HasColumnType("date");

            entity.Property(e => e.PricePerNight)
                .HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<SavedAccommodation>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AccommodationId }).HasName("PK__SavedAcc__1A33FCE99C8D0CDB");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Accommodation).WithMany(p => p.SavedAccommodations)
                .HasForeignKey(d => d.AccommodationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedAcco__Accom__607251E5");

            entity.HasOne(d => d.User).WithMany(p => p.SavedAccommodations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SavedAcco__UserI__5F7E2DAC");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.Email)
                .HasMaxLength(50);

            entity.Property(e => e.FullName)
                .HasMaxLength(50);

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.Role)
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");

            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false);

            entity.Property(e => e.LockReason)
                .HasMaxLength(255);

            entity.Property(e => e.LockedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.LastLoginAt)
                .HasColumnType("datetime");

            entity.Property(e => e.EmailConfirmed)
                .HasDefaultValue(false);
        });
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("UX_Promotions_Code");

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Title)
                .HasMaxLength(150);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(18, 2)");

            entity.Property(e => e.MaxDiscountAmount)
                .HasColumnType("decimal(18, 0)");

            entity.Property(e => e.MinBookingAmount)
                .HasColumnType("decimal(18, 0)")
                .HasDefaultValue(0);

            entity.Property(e => e.StartDate)
                .HasColumnType("datetime");

            entity.Property(e => e.EndDate)
                .HasColumnType("datetime");

            entity.Property(e => e.UsedCount)
                .HasDefaultValue(0);

            entity.Property(e => e.PerUserLimit)
                .HasDefaultValue(1);

            entity.Property(e => e.BannerImageUrl)
                .HasMaxLength(255);

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Promotions_Users");
        });

        modelBuilder.Entity<PromotionAccommodation>(entity =>
        {
            entity.HasKey(e => new { e.PromotionId, e.AccommodationId });

            entity.HasOne(d => d.Promotion)
                .WithMany(p => p.PromotionAccommodations)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_PromotionAccommodations_Promotions");

            entity.HasOne(d => d.Accommodation)
                .WithMany()
                .HasForeignKey(d => d.AccommodationId)
                .HasConstraintName("FK_PromotionAccommodations_Accommodations");
        });
        modelBuilder.Entity<RagDocument>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.SourceTable, e.SourceId })
                .IsUnique()
                .HasDatabaseName("UX_RagDocuments_Source");

            entity.Property(e => e.SourceTable)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.Title)
                .HasMaxLength(255);

            entity.Property(e => e.ContentHash)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.AiIndexStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Indexed");

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.IndexedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<RagChunk>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EmbeddingModel)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Document)
                .WithMany(p => p.RagChunks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("FK_RagChunks_RagDocuments")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BotQuestionLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Question)
                .HasMaxLength(1000);

            entity.Property(e => e.NormalizedQuestion)
                .HasMaxLength(1000);

            entity.Property(e => e.Intent)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
