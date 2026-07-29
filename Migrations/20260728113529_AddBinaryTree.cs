using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mlm.Migrations
{
    /// <inheritdoc />
    public partial class AddBinaryTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BinaryParentId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeftCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LeftLegId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RightCount",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RightLegId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BinaryPairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PairsMatched = table.Column<int>(type: "integer", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinaryPairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BinaryParentId",
                table: "AspNetUsers",
                column: "BinaryParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LeftLegId",
                table: "AspNetUsers",
                column: "LeftLegId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RightLegId",
                table: "AspNetUsers",
                column: "RightLegId");

            migrationBuilder.CreateIndex(
                name: "IX_BinaryPairs_UserId_CreatedAt",
                table: "BinaryPairs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BinaryParentId",
                table: "AspNetUsers",
                column: "BinaryParentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_LeftLegId",
                table: "AspNetUsers",
                column: "LeftLegId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_RightLegId",
                table: "AspNetUsers",
                column: "RightLegId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_BinaryParentId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_LeftLegId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_AspNetUsers_RightLegId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "BinaryPairs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BinaryParentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LeftLegId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RightLegId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BinaryParentId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LeftCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LeftLegId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RightCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RightLegId",
                table: "AspNetUsers");
        }
    }
}
