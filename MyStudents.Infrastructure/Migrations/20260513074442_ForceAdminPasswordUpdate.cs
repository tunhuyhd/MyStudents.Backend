using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForceAdminPasswordUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE users SET password_hash = '$2a$11$4HIlo6ImhI/LAIXHPdIxT.YkGP.2YaLGgUbLKOtU4mkL75hRu/1v2' WHERE id = '99999999-9999-9999-9999-999999999999';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
