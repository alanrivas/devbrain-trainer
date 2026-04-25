using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevBrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeOrderingItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "items",
                table: "challenges",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                column: "items",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                column: "items",
                value: null);

            migrationBuilder.InsertData(
                table: "challenges",
                columns: new[] { "Id", "category", "CorrectAnswer", "CreatedAt", "Description", "difficulty", "items", "options", "starter_code", "test_cases", "TimeLimitSecs", "Title", "type" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), 2, "Domain|Application|Infrastructure|UI", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Order the layers of a hexagonal architecture from innermost to outermost.", 1, "UI|Infrastructure|Domain|Application", null, "", null, 90, "Architecture: Hexagonal Layers", 3 },
                    { new Guid("40000000-0000-0000-0000-000000000002"), 3, "Build|Test|Lint|Deploy", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Order the typical stages of a CI/CD pipeline from first to last.", 0, "Deploy|Lint|Build|Test", null, "", null, 75, "DevOps: CI/CD Pipeline Stages", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DropColumn(
                name: "items",
                table: "challenges");
        }
    }
}
