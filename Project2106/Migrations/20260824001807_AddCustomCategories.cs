using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PROJECT2106.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SystemCategory",
                table: "Places",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "CustomCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomCategories_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Places_CustomCategoryId",
                table: "Places",
                column: "CustomCategoryId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Places_Category_ExactlyOne",
                table: "Places",
                sql: "(\"SystemCategory\" IS NOT NULL AND \"CustomCategoryId\" IS NULL) OR (\"SystemCategory\" IS NULL AND \"CustomCategoryId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Places_SystemCategory_Range",
                table: "Places",
                sql: "\"SystemCategory\" IS NULL OR (\"SystemCategory\" >= 0 AND \"SystemCategory\" <= 8)");

            migrationBuilder.CreateIndex(
                name: "IX_CustomCategories_CreatedByUserId",
                table: "CustomCategories",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Places_CustomCategories_CustomCategoryId",
                table: "Places",
                column: "CustomCategoryId",
                principalTable: "CustomCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Places_CustomCategories_CustomCategoryId",
                table: "Places");

            migrationBuilder.DropTable(
                name: "CustomCategories");

            migrationBuilder.DropIndex(
                name: "IX_Places_CustomCategoryId",
                table: "Places");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Places_Category_ExactlyOne",
                table: "Places");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Places_SystemCategory_Range",
                table: "Places");

            migrationBuilder.AlterColumn<int>(
                name: "SystemCategory",
                table: "Places",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
