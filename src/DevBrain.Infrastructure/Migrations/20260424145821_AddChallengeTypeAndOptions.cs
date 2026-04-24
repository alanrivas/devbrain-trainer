using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevBrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeTypeAndOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "options",
                table: "challenges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "options",
                value: null);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                column: "options",
                value: null);

            migrationBuilder.InsertData(
                table: "challenges",
                columns: new[] { "Id", "category", "CorrectAnswer", "CreatedAt", "Description", "difficulty", "options", "TimeLimitSecs", "Title", "type" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), 1, "O(log n)", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "What is the time complexity of binary search on a sorted array of n elements?", 1, "O(n)|O(n²)|O(log n)|O(n log n)", 60, "Algorithm: Binary Search Complexity", 1 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), 0, "INNER JOIN", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Which JOIN returns only rows that have matching values in BOTH tables?", 0, "LEFT JOIN|RIGHT JOIN|INNER JOIN|FULL OUTER JOIN", 45, "SQL: JOIN Type", 1 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), 2, "Strategy", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Which design pattern defines a family of algorithms, encapsulates each one, and makes them interchangeable?", 1, "Observer|Strategy|Command|Decorator", 75, "Architecture: Strategy Pattern", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_challenges_type",
                table: "challenges",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_challenges_type",
                table: "challenges");

            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DropColumn(
                name: "options",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "type",
                table: "challenges");
        }
    }
}
