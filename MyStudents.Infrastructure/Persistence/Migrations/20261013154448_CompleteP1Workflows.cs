using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteP1Workflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_class_students_class_id",
                table: "class_students");

            migrationBuilder.DropIndex(
                name: "IX_class_sessions_class_id",
                table: "class_sessions");

            migrationBuilder.DropIndex(
                name: "IX_attendances_session_id",
                table: "attendances");

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_reset_requested_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token_hash",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_manual",
                table: "class_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                columns: new[] { "password_reset_expires_at", "password_reset_requested_at", "password_reset_token_hash" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_users_password_reset_token_hash",
                table: "users",
                column: "password_reset_token_hash",
                unique: true,
                filter: "password_reset_token_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_class_students_class_id_student_id",
                table: "class_students",
                columns: new[] { "class_id", "student_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_class_id_date_start_time",
                table: "class_sessions",
                columns: new[] { "class_id", "date", "start_time" },
                unique: true,
                filter: "is_deleted = false AND status <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_id_student_id",
                table: "attendances",
                columns: new[] { "session_id", "student_id" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_password_reset_token_hash",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_class_students_class_id_student_id",
                table: "class_students");

            migrationBuilder.DropIndex(
                name: "IX_class_sessions_class_id_date_start_time",
                table: "class_sessions");

            migrationBuilder.DropIndex(
                name: "IX_attendances_session_id_student_id",
                table: "attendances");

            migrationBuilder.DropColumn(
                name: "password_reset_expires_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_requested_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_manual",
                table: "class_sessions");

            migrationBuilder.CreateIndex(
                name: "IX_class_students_class_id",
                table: "class_students",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_class_id",
                table: "class_sessions",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_id",
                table: "attendances",
                column: "session_id");
        }
    }
}
