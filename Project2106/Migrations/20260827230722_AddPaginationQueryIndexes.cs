using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT2106.Migrations
{
    /// <inheritdoc />
    public partial class AddPaginationQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PlaceId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Places_CreatedByUserId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Likes_PostId",
                table: "Likes");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId_CreatedAt",
                table: "Posts",
                columns: new[] { "AuthorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PlaceId_CreatedAt",
                table: "Posts",
                columns: new[] { "PlaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Places_CreatedByUserId_CreatedAt",
                table: "Places",
                columns: new[] { "CreatedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_PostId_IsLike",
                table: "Likes",
                columns: new[] { "PostId", "IsLike" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_AuthorId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PlaceId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Places_CreatedByUserId_CreatedAt",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Likes_PostId_IsLike",
                table: "Likes");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PlaceId",
                table: "Posts",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_CreatedByUserId",
                table: "Places",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_PostId",
                table: "Likes",
                column: "PostId");
        }
    }
}
