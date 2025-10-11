using FatSecret.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace FatSecret.DAL.Context;

public class FatSecretDbContext : DbContext
{
    public FatSecretDbContext(DbContextOptions<FatSecretDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<WeightRecord> WeightRecords { get; set; }
    public DbSet<FoodEntry> FoodEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntity(modelBuilder);
        ConfigureUserSessionEntity(modelBuilder);
        ConfigureUserPreferencesEntity(modelBuilder);
        ConfigureWeightRecordEntity(modelBuilder);
        ConfigureFoodEntryEntity(modelBuilder);
    }

    #region User Configuration

    private static void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        // Таблица
        entity.ToTable("Users");

        // Первичный ключ
        entity.HasKey(u => u.Id);
        entity.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Уникальные ограничения
        entity.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        entity.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        entity.HasIndex(u => u.EmailVerificationToken)
            .HasDatabaseName("IX_Users_EmailVerificationToken");

        entity.HasIndex(u => u.PasswordResetToken)
            .HasDatabaseName("IX_Users_PasswordResetToken");

        // Основные поля
        entity.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(u => u.PasswordSalt)
            .HasColumnName("password_salt")
            .HasMaxLength(255);

        // Физические параметры
        entity.Property(u => u.Age)
            .HasColumnName("age")
            .HasDefaultValue(0);

        entity.Property(u => u.Weight)
            .HasColumnName("weight")
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0m);

        entity.Property(u => u.Height)
            .HasColumnName("height")
            .HasDefaultValue(0);

        entity.Property(u => u.Gender)
            .HasColumnName("gender")
            .HasMaxLength(20);

        // Цели и активность
        entity.Property(u => u.Goal)
            .HasColumnName("goal")
            .HasMaxLength(20)
            .HasDefaultValue("maintain")
            .IsRequired();

        entity.Property(u => u.ActivityLevel)
            .HasColumnName("activity_level")
            .HasMaxLength(20)
            .HasDefaultValue("moderate")
            .IsRequired();

        // Расчетные значения
        entity.Property(u => u.BasalMetabolicRate)
            .HasColumnName("basal_metabolic_rate")
            .HasDefaultValue(0);

        entity.Property(u => u.DailyCalorieTarget)
            .HasColumnName("daily_calorie_target")
            .HasDefaultValue(0);

        // Дополнительная информация
        entity.Property(u => u.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        // Системные поля
        entity.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entity.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        entity.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        entity.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        entity.Property(u => u.EmailVerified)
            .HasColumnName("email_verified")
            .HasDefaultValue(false);

        entity.Property(u => u.EmailVerificationToken)
            .HasColumnName("email_verification_token")
            .HasMaxLength(255);

        entity.Property(u => u.PasswordResetToken)
            .HasColumnName("password_reset_token")
            .HasMaxLength(255);

        entity.Property(u => u.PasswordResetExpires)
            .HasColumnName("password_reset_expires");

        // Навигационные свойства
        entity.HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.FoodEntries)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.WeightRecords)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(u => u.Preferences)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ограничения
        // entity.HasCheckConstraint("CK_Users_Age", "[age] >= 0 AND [age] <= 150");
        // entity.HasCheckConstraint("CK_Users_Weight", "[weight] >= 0 AND [weight] <= 1000");
        // entity.HasCheckConstraint("CK_Users_Height", "[height] >= 0 AND [height] <= 300");
        // entity.HasCheckConstraint("CK_Users_BMR", "[basal_metabolic_rate] >= 0");
        // entity.HasCheckConstraint("CK_Users_CalorieTarget", "[daily_calorie_target] >= 0");
    }

    #endregion

    #region UserSession Configuration

    private static void ConfigureUserSessionEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserSession>();

        // Таблица
        entity.ToTable("UserSessions");

        // Первичный ключ
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Индексы
        entity.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        entity.HasIndex(s => s.TokenId)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_TokenId");

        entity.HasIndex(s => new { s.UserId, s.IsRevoked })
            .HasDatabaseName("IX_UserSessions_UserId_IsRevoked");

        entity.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_UserSessions_ExpiresAt");

        // Поля
        entity.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        entity.Property(s => s.TokenId)
            .HasColumnName("token_id")
            .HasMaxLength(255)
            .IsRequired();

        entity.Property(s => s.DeviceInfo)
            .HasColumnName("device_info")
            .HasMaxLength(500);

        entity.Property(s => s.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); // Достаточно для IPv6

        entity.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entity.Property(s => s.LastActivity)
            .HasColumnName("last_activity")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entity.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        entity.Property(s => s.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false);

        entity.Property(s => s.RevokedAt)
            .HasColumnName("revoked_at");

        // Внешние ключи
        entity.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    #endregion

    #region UserPreferences Configuration

    private static void ConfigureUserPreferencesEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserPreferences>();

        // Таблица
        entity.ToTable("UserPreferences");

        // Первичный ключ
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Уникальный индекс по UserId
        entity.HasIndex(p => p.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserPreferences_UserId");

        // Поля
        entity.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Настройки уведомлений
        entity.Property(p => p.EmailNotifications)
            .HasColumnName("email_notifications")
            .HasDefaultValue(true);

        entity.Property(p => p.DailyReminder)
            .HasColumnName("daily_reminder")
            .HasDefaultValue(true);

        entity.Property(p => p.WeeklySummary)
            .HasColumnName("weekly_summary")
            .HasDefaultValue(true);

        // Настройки приватности
        entity.Property(p => p.ProfilePublic)
            .HasColumnName("profile_public")
            .HasDefaultValue(false);

        entity.Property(p => p.StatsPublic)
            .HasColumnName("stats_public")
            .HasDefaultValue(false);

        // Региональные настройки
        entity.Property(p => p.Language)
            .HasColumnName("language")
            .HasMaxLength(10)
            .HasDefaultValue("ru");

        entity.Property(p => p.Timezone)
            .HasColumnName("timezone")
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        entity.Property(p => p.DateFormat)
            .HasColumnName("date_format")
            .HasMaxLength(20)
            .HasDefaultValue("dd.MM.yyyy");

        entity.Property(p => p.WeightUnit)
            .HasColumnName("weight_unit")
            .HasMaxLength(10)
            .HasDefaultValue("kg");

        entity.Property(p => p.HeightUnit)
            .HasColumnName("height_unit")
            .HasMaxLength(10)
            .HasDefaultValue("cm");

        // Системные поля
        entity.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        entity.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        // Внешние ключи
        entity.HasOne(p => p.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    #endregion

    #region WeightRecord Configuration

    private static void ConfigureWeightRecordEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WeightRecord>();

        // Таблица
        entity.ToTable("WeightRecords");

        // Первичный ключ
        entity.HasKey(w => w.Id);
        entity.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Индексы
        entity.HasIndex(w => w.UserId)
            .HasDatabaseName("IX_WeightRecords_UserId");

        entity.HasIndex(w => new { w.UserId, w.RecordedDate })
            .IsUnique()
            .HasDatabaseName("IX_WeightRecords_UserId_RecordedDate");

        entity.HasIndex(w => w.RecordedDate)
            .HasDatabaseName("IX_WeightRecords_RecordedDate");

        // Поля
        entity.Property(w => w.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        entity.Property(w => w.Weight)
            .HasColumnName("weight")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        entity.Property(w => w.RecordedDate)
            .HasColumnName("recorded_date")
            .HasColumnType("date")
            .IsRequired();

        entity.Property(w => w.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        entity.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Внешние ключи
        entity.HasOne(w => w.User)
            .WithMany(u => u.WeightRecords)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ограничения
        //entity.HasCheckConstraint("CK_WeightRecords_Weight", "[weight] > 0 AND [weight] <= 1000");
    }

    #endregion

    #region FoodEntry Configuration

    private static void ConfigureFoodEntryEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FoodEntry>();

        // Таблица
        entity.ToTable("FoodEntries");

        // Первичный ключ
        entity.HasKey(f => f.Id);
        entity.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Индексы
        entity.HasIndex(f => f.UserId)
            .HasDatabaseName("IX_FoodEntries_UserId");

        entity.HasIndex(f => new { f.UserId, f.EntryDate })
            .HasDatabaseName("IX_FoodEntries_UserId_EntryDate");

        entity.HasIndex(f => f.EntryDate)
            .HasDatabaseName("IX_FoodEntries_EntryDate");

        entity.HasIndex(f => new { f.UserId, f.MealType, f.EntryDate })
            .HasDatabaseName("IX_FoodEntries_UserId_MealType_EntryDate");

        // Поля
        entity.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        entity.Property(f => f.FoodName)
            .HasColumnName("food_name")
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(f => f.Calories)
            .HasColumnName("calories")
            .IsRequired();

        entity.Property(f => f.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(8,2)")
            .HasDefaultValue(0m);

        entity.Property(f => f.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50)
            .HasDefaultValue("г");

        entity.Property(f => f.EntryDate)
            .HasColumnName("entry_date")
            .HasColumnType("date")
            .IsRequired();

        entity.Property(f => f.MealType)
            .HasColumnName("meal_type")
            .HasMaxLength(20)
            .HasDefaultValue("other")
            .IsRequired();

        entity.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Внешние ключи
        entity.HasOne(f => f.User)
            .WithMany(u => u.FoodEntries)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ограничения
        //entity.HasCheckConstraint("CK_FoodEntries_Calories", "[calories] >= 0");
        //entity.HasCheckConstraint("CK_FoodEntries_Quantity", "[quantity] >= 0");
    }

    #endregion
}