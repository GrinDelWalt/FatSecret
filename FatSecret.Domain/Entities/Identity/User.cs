using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatSecret.Domain.Entities.Identity;

/// <summary>
/// Модель пользователя с расширенными данными для дневника питания
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(255)]
    [Column("password_salt")]
    public string? PasswordSalt { get; set; }

    // Физические параметры
    [Column("age")]
    public int Age { get; set; }

    [Column("weight", TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    [Column("height")]
    public int Height { get; set; }

    [StringLength(20)]
    [Column("gender")]
    public string? Gender { get; set; }

    // Цели и активность
    [Required]
    [StringLength(20)]
    [Column("goal")]
    public string Goal { get; set; } = "maintain";

    [Required]
    [StringLength(20)]
    [Column("activity_level")]
    public string ActivityLevel { get; set; } = "moderate";

    // Расчетные значения
    [Column("basal_metabolic_rate")]
    public int BasalMetabolicRate { get; set; }

    [Column("daily_calorie_target")]
    public int DailyCalorieTarget { get; set; }

    // Дополнительная информация
    [StringLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    // Системные поля
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;

    [Column("email_verification_token")]
    public string? EmailVerificationToken { get; set; }

    [Column("password_reset_token")]
    public string? PasswordResetToken { get; set; }

    [Column("password_reset_expires")]
    public DateTime? PasswordResetExpires { get; set; }

    // Навигационные свойства
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<FoodEntry> FoodEntries { get; set; } = new List<FoodEntry>();
    public virtual ICollection<WeightRecord> WeightRecords { get; set; } = new List<WeightRecord>();
    public virtual UserPreferences? Preferences { get; set; }

    // Методы для работы с данными
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string token, TimeSpan expiry)
    {
        PasswordResetToken = token;
        PasswordResetExpires = DateTime.UtcNow.Add(expiry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpires = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsPasswordResetTokenValid()
    {
        return !string.IsNullOrEmpty(PasswordResetToken) && 
               PasswordResetExpires.HasValue && 
               PasswordResetExpires.Value > DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        EmailVerificationToken = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Модель сессии пользователя
/// </summary>
[Table("UserSessions")]
public class UserSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("token_id")]
    public string TokenId { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("device_info")]
    public string? DeviceInfo { get; set; }

    [StringLength(45)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; } = false;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;
}

/// <summary>
/// Модель предпочтений пользователя
/// </summary>
[Table("UserPreferences")]
public class UserPreferences
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    // Настройки уведомлений
    [Column("email_notifications")]
    public bool EmailNotifications { get; set; } = true;

    [Column("daily_reminder")]
    public bool DailyReminder { get; set; } = true;

    [Column("weekly_summary")]
    public bool WeeklySummary { get; set; } = true;

    // Настройки приватности
    [Column("profile_public")]
    public bool ProfilePublic { get; set; } = false;

    [Column("stats_public")]
    public bool StatsPublic { get; set; } = false;

    // Региональные настройки
    [StringLength(10)]
    [Column("language")]
    public string Language { get; set; } = "ru";

    [StringLength(10)]
    [Column("timezone")]
    public string Timezone { get; set; } = "UTC";

    [StringLength(10)]
    [Column("date_format")]
    public string DateFormat { get; set; } = "dd.MM.yyyy";

    [StringLength(10)]
    [Column("weight_unit")]
    public string WeightUnit { get; set; } = "kg";

    [StringLength(10)]
    [Column("height_unit")]
    public string HeightUnit { get; set; } = "cm";

    // Системные поля
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Модель записи веса пользователя
/// </summary>
[Table("WeightRecords")]
public class WeightRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("weight", TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; }

    [Required]
    [Column("recorded_date")]
    public DateOnly RecordedDate { get; set; }

    [StringLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Базовая модель записи о еде (упрощенная для примера)
/// </summary>
[Table("FoodEntries")]
public class FoodEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    [Column("food_name")]
    public string FoodName { get; set; } = string.Empty;

    [Required]
    [Column("calories")]
    public int Calories { get; set; }

    [Column("quantity", TypeName = "decimal(8,2)")]
    public decimal Quantity { get; set; }

    [StringLength(50)]
    [Column("unit")]
    public string Unit { get; set; } = "г";

    [Required]
    [Column("entry_date")]
    public DateOnly EntryDate { get; set; }

    [Required]
    [StringLength(20)]
    [Column("meal_type")]
    public string MealType { get; set; } = "other"; // breakfast, lunch, dinner, snack, other

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}