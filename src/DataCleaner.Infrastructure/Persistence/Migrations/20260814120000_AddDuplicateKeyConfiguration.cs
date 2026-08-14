using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace DataCleaner.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DataCleanerDbContext))]
[Migration("20260814120000_AddDuplicateKeyConfiguration")]
public partial class AddDuplicateKeyConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsDuplicateKey",
            table: "ColumnMappings",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsDuplicateKey",
            table: "ColumnMappings");
    }
}
