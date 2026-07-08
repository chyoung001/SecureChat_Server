using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureChat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastReadAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadAt",
                table: "RoomMembers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "RoomMembers");
        }
    }
}
