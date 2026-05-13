using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "password_hash",
                value: "$2a$11$2v972T5SL5Vr1NQjsrjTvukoawPfRibO2xaZHaglroKzLU9dUBnUu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "password_hash",
                value: "$2a$11$R9h/lS76P5SfBvY.P3A7Pe3Zf7U5u9Z5z8w6bX.v3V5O5U5U5U5U5U5U5U");
        }
    }
}
