using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevBrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeCodeRunnerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "starter_code",
                table: "challenges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "test_cases",
                table: "challenges",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"),
                columns: new[] { "starter_code", "test_cases" },
                values: new object[] { "", null });

            migrationBuilder.InsertData(
                table: "challenges",
                columns: new[] { "Id", "category", "CorrectAnswer", "CreatedAt", "Description", "difficulty", "options", "starter_code", "test_cases", "TimeLimitSecs", "Title", "type" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), 1, "PASS", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Write a JavaScript function `solution(a, b)` that returns the sum of two numbers.", 0, null, "function solution(a, b) {\n  // Write your code here\n}", "[{\"Input\":\"2, 3\",\"ExpectedOutput\":\"5\",\"Description\":\"Returns sum of 2 and 3\"},{\"Input\":\"0, 0\",\"ExpectedOutput\":\"0\",\"Description\":\"Returns sum of 0 and 0\"},{\"Input\":\"-1, 1\",\"ExpectedOutput\":\"0\",\"Description\":\"Returns sum of -1 and 1\"}]", 120, "JS: Sum Two Numbers", 2 },
                    { new Guid("30000000-0000-0000-0000-000000000002"), 1, "PASS", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Write a JavaScript function `solution(arr)` that returns a new array containing only the even numbers from the input array.", 0, null, "function solution(arr) {\n  // Write your code here\n}", "[{\"Input\":\"[1, 2, 3, 4]\",\"ExpectedOutput\":\"2,4\",\"Description\":\"Filters even numbers from mixed array\"},{\"Input\":\"[0, 1, 2]\",\"ExpectedOutput\":\"0,2\",\"Description\":\"Includes zero as even\"},{\"Input\":\"[2, 4, 6]\",\"ExpectedOutput\":\"2,4,6\",\"Description\":\"Returns all elements when all are even\"}]", 120, "JS: Filter Even Numbers", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "challenges",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DropColumn(
                name: "starter_code",
                table: "challenges");

            migrationBuilder.DropColumn(
                name: "test_cases",
                table: "challenges");
        }
    }
}
