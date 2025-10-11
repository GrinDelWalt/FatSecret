using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FatSecret.DAL.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WeightRecords_Weight",
                table: "WeightRecords");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Age",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_BMR",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_CalorieTarget",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Height",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Weight",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FoodEntries_Calories",
                table: "FoodEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FoodEntries_Quantity",
                table: "FoodEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_WeightRecords_Weight",
                table: "WeightRecords",
                sql: "weight > 0 AND weight <= 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Age",
                table: "Users",
                sql: "age >= 0 AND age <= 150");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_BMR",
                table: "Users",
                sql: "basal_metabolic_rate >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_CalorieTarget",
                table: "Users",
                sql: "daily_calorie_target >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Height",
                table: "Users",
                sql: "height >= 0 AND height <= 300");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Weight",
                table: "Users",
                sql: "weight >= 0 AND weight <= 1000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FoodEntries_Calories",
                table: "FoodEntries",
                sql: "calories >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FoodEntries_Quantity",
                table: "FoodEntries",
                sql: "quantity >= 0");
        }
    }
}
