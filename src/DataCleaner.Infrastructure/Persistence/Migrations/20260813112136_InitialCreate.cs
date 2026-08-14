using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataCleaner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProcessedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    InvalidRows = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProfileVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CultureName = table.Column<string>(type: "TEXT", nullable: true),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: true),
                    NumberFormat = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValidRows = table.Column<int>(type: "INTEGER", nullable: false),
                    ModifiedRows = table.Column<int>(type: "INTEGER", nullable: false),
                    DuplicatesRemoved = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportResults_ImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "ImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CleaningRuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningRuleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CleaningRuleConfigurations_ImportProfiles_ImportProfileId",
                        column: x => x.ImportProfileId,
                        principalTable: "ImportProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColumnMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceColumn = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    TargetField = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsIgnored = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColumnMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColumnMappings_ImportProfiles_ImportProfileId",
                        column: x => x.ImportProfileId,
                        principalTable: "ImportProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRuleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationRuleConfigurations_ImportProfiles_ImportProfileId",
                        column: x => x.ImportProfileId,
                        principalTable: "ImportProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningRuleConfigurations_ImportProfileId",
                table: "CleaningRuleConfigurations",
                column: "ImportProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ColumnMappings_ImportProfileId",
                table: "ColumnMappings",
                column: "ImportProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportProfiles_Name",
                table: "ImportProfiles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportResults_ImportJobId",
                table: "ImportResults",
                column: "ImportJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuleConfigurations_ImportProfileId",
                table: "ValidationRuleConfigurations",
                column: "ImportProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CleaningRuleConfigurations");

            migrationBuilder.DropTable(
                name: "ColumnMappings");

            migrationBuilder.DropTable(
                name: "ImportResults");

            migrationBuilder.DropTable(
                name: "ValidationRuleConfigurations");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "ImportProfiles");
        }
    }
}
