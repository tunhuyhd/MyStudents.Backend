using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalAdminHashFixV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "password_hash",
                value: "$2a$11$4HIlo6ImhI/LAIXHPdIxT.YkGP.2YaLGgUbLKOtU4mkL75hRu/1v2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "password_hash",
                value: "$2a$11$r.N2dAT0gnA9i2QlUlmGNu.7fpVSDOUwzJknL7ZCVd0ln04j71u1y");
        }
    }
}
