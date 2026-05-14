using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClassSessionAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_ClassSessions_session_id",
                table: "ClassSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_students_student_id",
                table: "ClassSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSessions",
                table: "ClassSessions");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_session_id",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "session_id",
                table: "ClassSessions");

            migrationBuilder.RenameTable(
                name: "ClassSessions",
                newName: "class_sessions");

            migrationBuilder.RenameColumn(
                name: "student_id",
                table: "class_sessions",
                newName: "class_id");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSessions_student_id",
                table: "class_sessions",
                newName: "IX_class_sessions_class_id");

            migrationBuilder.AddColumn<DateOnly>(
                name: "date",
                table: "class_sessions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "end_time",
                table: "class_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "schedule_id",
                table: "class_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "start_time",
                table: "class_sessions",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddPrimaryKey(
                name: "PK_class_sessions",
                table: "class_sessions",
                column: "id");

            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendances_class_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "class_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attendances_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_schedule_id",
                table: "class_sessions",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_session_id",
                table: "attendances",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_student_id",
                table: "attendances",
                column: "student_id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_sessions_ClassSchedules_schedule_id",
                table: "class_sessions",
                column: "schedule_id",
                principalTable: "ClassSchedules",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_sessions_classes_class_id",
                table: "class_sessions",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_sessions_ClassSchedules_schedule_id",
                table: "class_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_class_sessions_classes_class_id",
                table: "class_sessions");

            migrationBuilder.DropTable(
                name: "attendances");

            migrationBuilder.DropPrimaryKey(
                name: "PK_class_sessions",
                table: "class_sessions");

            migrationBuilder.DropIndex(
                name: "IX_class_sessions_schedule_id",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "date",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "schedule_id",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "start_time",
                table: "class_sessions");

            migrationBuilder.RenameTable(
                name: "class_sessions",
                newName: "ClassSessions");

            migrationBuilder.RenameColumn(
                name: "class_id",
                table: "ClassSessions",
                newName: "student_id");

            migrationBuilder.RenameIndex(
                name: "IX_class_sessions_class_id",
                table: "ClassSessions",
                newName: "IX_ClassSessions_student_id");

            migrationBuilder.AddColumn<Guid>(
                name: "session_id",
                table: "ClassSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSessions",
                table: "ClassSessions",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_session_id",
                table: "ClassSessions",
                column: "session_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_ClassSessions_session_id",
                table: "ClassSessions",
                column: "session_id",
                principalTable: "ClassSessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_students_student_id",
                table: "ClassSessions",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
