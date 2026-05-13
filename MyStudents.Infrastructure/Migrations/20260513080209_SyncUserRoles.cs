using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "description", "name" },
                values: new object[] { "Administrator role", "Admin" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "description", "name" },
                values: new object[] { "User role", "User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "description", "name" },
                values: new object[] { "Administrator with full access", "ADMIN" });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "description", "name" },
                values: new object[] { "Regular user with limited access", "USER" });
        }
    }
}
