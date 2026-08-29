using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PROJECT2106.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaceId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SystemCategory = table.Column<int>(type: "integer", nullable: false),
                    CustomCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.CheckConstraint("CK_Places_Latitude", "\"Latitude\" >= -90 AND \"Latitude\" <= 90");
                    table.CheckConstraint("CK_Places_Longitude", "\"Longitude\" >= -180 AND \"Longitude\" <= 180");
                    table.CheckConstraint("CK_Places_Name_NotBlank", "length(btrim(\"Name\")) > 0");
                    table.ForeignKey(
                        name: "FK_Places_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PlaceId",
                table: "Posts",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_CreatedByUserId",
                table: "Places",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Places_PlaceId",
                table: "Posts",
                column: "PlaceId",
                principalTable: "Places",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Places_PlaceId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PlaceId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                table: "Posts");
        }
    }
}
