using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAccountSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Invalidate refresh tokens issued before server-side session checks.
            migrationBuilder.Sql("UPDATE users SET refresh_token = NULL, refresh_token_expiry_time = NULL;");

            // Disable only the legacy seeded credential. Never overwrite an
            // administrator password that an operator has already changed.
            migrationBuilder.Sql("""
                UPDATE users SET password_hash = '', status = 2
                WHERE id = '99999999-9999-9999-9999-999999999999'
                  AND password_hash = '$2a$11$4HIlo6ImhI/LAIXHPdIxT.YkGP.2YaLGgUbLKOtU4mkL75hRu/1v2';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "users");

            // Deliberately do not restore the publicly known admin credential.
        }
    }
}
