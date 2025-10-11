using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FatSecret.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_salt = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    age = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    height = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    goal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "maintain"),
                    activity_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "moderate"),
                    basal_metabolic_rate = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    daily_calorie_target = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email_verification_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_reset_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_reset_expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                    table.CheckConstraint("CK_Users_Age", "age >= 0 AND age <= 150");
                    table.CheckConstraint("CK_Users_BMR", "basal_metabolic_rate >= 0");
                    table.CheckConstraint("CK_Users_CalorieTarget", "daily_calorie_target >= 0");
                    table.CheckConstraint("CK_Users_Height", "height >= 0 AND height <= 300");
                    table.CheckConstraint("CK_Users_Weight", "weight >= 0 AND weight <= 1000");
                });

            migrationBuilder.CreateTable(
                name: "FoodEntries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    food_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    calories = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(8,2)", nullable: false, defaultValue: 0m),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "г"),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "other"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodEntries", x => x.id);
                    table.CheckConstraint("CK_FoodEntries_Calories", "calories >= 0");
                    table.CheckConstraint("CK_FoodEntries_Quantity", "quantity >= 0");
                    table.ForeignKey(
                        name: "FK_FoodEntries_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    daily_reminder = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    weekly_summary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    profile_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    stats_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ru"),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    date_format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "dd.MM.yyyy"),
                    weight_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "kg"),
                    height_unit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "cm"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_activity = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightRecords",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    recorded_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightRecords", x => x.id);
                    table.CheckConstraint("CK_WeightRecords_Weight", "weight > 0 AND weight <= 1000");
                    table.ForeignKey(
                        name: "FK_WeightRecords_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_EntryDate",
                table: "FoodEntries",
                column: "entry_date");

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_UserId",
                table: "FoodEntries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_UserId_EntryDate",
                table: "FoodEntries",
                columns: new[] { "user_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "IX_FoodEntries_UserId_MealType_EntryDate",
                table: "FoodEntries",
                columns: new[] { "user_id", "meal_type", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users",
                column: "email_verification_token");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users",
                column: "password_reset_token");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_TokenId",
                table: "UserSessions",
                column: "token_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsRevoked",
                table: "UserSessions",
                columns: new[] { "user_id", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "IX_WeightRecords_RecordedDate",
                table: "WeightRecords",
                column: "recorded_date");

            migrationBuilder.CreateIndex(
                name: "IX_WeightRecords_UserId",
                table: "WeightRecords",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_WeightRecords_UserId_RecordedDate",
                table: "WeightRecords",
                columns: new[] { "user_id", "recorded_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodEntries");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "WeightRecords");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
