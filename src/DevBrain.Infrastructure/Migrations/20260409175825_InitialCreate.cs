using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevBrain.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "text", nullable: false),
                    TimeLimitSecs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAnswer = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    ElapsedSecs = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attempts_challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attempts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "challenges",
                columns: new[] { "Id", "category", "CorrectAnswer", "CreatedAt", "Description", "difficulty", "TimeLimitSecs", "Title" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 0, "SELECT TOP 5 u.id, COUNT(a.id) as attempt_count FROM users u LEFT JOIN attempts a ON u.id = a.user_id GROUP BY u.id ORDER BY attempt_count DESC", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Write a SQL query that returns the top 5 users by number of completed attempts", 0, 60, "SQL: Select Top N Records" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 0, "SELECT u.id, u.email, c.title, a.created_at FROM users u LEFT JOIN attempts a ON u.id = a.user_id LEFT JOIN challenges c ON a.challenge_id = c.id WHERE a.created_at = (SELECT MAX(created_at) FROM attempts WHERE user_id = u.id)", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Write a query that joins users with their latest attempt, including the challenge title", 1, 120, "SQL: Join Multiple Tables" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 1, "private void CheckAndPrint(int value) { if (value > 10) Print(\"big\"); }", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Refactor this code to follow DRY: if (x > 10) print(\"big\"); if (y > 10) print(\"big\");", 0, 90, "C#: Extract Method" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 1, "??", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "What operator returns the first non-null value in C#?", 0, 45, "C#: Null Coalescing" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 2, "single responsibility principle", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Which SOLID principle states that a class should have only one reason to change?", 1, 75, "Architecture: SOLID - Single Responsibility" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 2, "singleton", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "What design pattern restricts object instantiation to a single instance?", 2, 150, "Architecture: Design Pattern" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 3, "docker ps -a", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "What Docker command lists all containers (running and stopped)?", 0, 60, "Docker: Container Listing" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), 3, "docker image prune", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "What Docker command removes all unused images?", 1, 90, "Docker: Image Cleanup" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), 4, "15", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Trace this code: x = 5; x += 3; x *= 2; x -= 1; What is the final value of x?", 1, 120, "Memory: Variable Tracing" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), 4, "7", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Count how many times this loop executes: for (int i = 0; i < 20; i += 3) {}", 0, 60, "Memory: Loop Counting" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_attempts_ChallengeId",
                table: "attempts",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_UserId",
                table: "attempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_UserId_ChallengeId_IsCorrect",
                table: "attempts",
                columns: new[] { "UserId", "ChallengeId", "IsCorrect" });

            migrationBuilder.CreateIndex(
                name: "IX_attempts_UserId_OccurredAt",
                table: "attempts",
                columns: new[] { "UserId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_challenges_category",
                table: "challenges",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_challenges_difficulty",
                table: "challenges",
                column: "difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempts");

            migrationBuilder.DropTable(
                name: "challenges");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
