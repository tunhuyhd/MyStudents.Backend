using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_sessions_ClassSchedules_schedule_id",
                table: "class_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSchedules_classes_class_id",
                table: "ClassSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassStudents_classes_class_id",
                table: "ClassStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassStudents_students_student_id",
                table: "ClassStudents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassStudents",
                table: "ClassStudents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClassSchedules",
                table: "ClassSchedules");

            migrationBuilder.RenameTable(
                name: "ClassStudents",
                newName: "class_students");

            migrationBuilder.RenameTable(
                name: "ClassSchedules",
                newName: "class_schedules");

            migrationBuilder.RenameIndex(
                name: "IX_ClassStudents_student_id",
                table: "class_students",
                newName: "IX_class_students_student_id");

            migrationBuilder.RenameIndex(
                name: "IX_ClassStudents_class_id",
                table: "class_students",
                newName: "IX_class_students_class_id");

            migrationBuilder.RenameIndex(
                name: "IX_ClassSchedules_class_id",
                table: "class_schedules",
                newName: "IX_class_schedules_class_id");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_class_students",
                table: "class_students",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_class_schedules",
                table: "class_schedules",
                column: "id");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"),
                column: "status",
                value: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_class_schedules_classes_class_id",
                table: "class_schedules",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_class_sessions_class_schedules_schedule_id",
                table: "class_sessions",
                column: "schedule_id",
                principalTable: "class_schedules",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_students_classes_class_id",
                table: "class_students",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_class_students_students_student_id",
                table: "class_students",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_schedules_classes_class_id",
                table: "class_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_class_sessions_class_schedules_schedule_id",
                table: "class_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_class_students_classes_class_id",
                table: "class_students");

            migrationBuilder.DropForeignKey(
                name: "FK_class_students_students_student_id",
                table: "class_students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_class_students",
                table: "class_students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_class_schedules",
                table: "class_schedules");

            migrationBuilder.DropColumn(
                name: "status",
                table: "users");

            migrationBuilder.RenameTable(
                name: "class_students",
                newName: "ClassStudents");

            migrationBuilder.RenameTable(
                name: "class_schedules",
                newName: "ClassSchedules");

            migrationBuilder.RenameIndex(
                name: "IX_class_students_student_id",
                table: "ClassStudents",
                newName: "IX_ClassStudents_student_id");

            migrationBuilder.RenameIndex(
                name: "IX_class_students_class_id",
                table: "ClassStudents",
                newName: "IX_ClassStudents_class_id");

            migrationBuilder.RenameIndex(
                name: "IX_class_schedules_class_id",
                table: "ClassSchedules",
                newName: "IX_ClassSchedules_class_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassStudents",
                table: "ClassStudents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClassSchedules",
                table: "ClassSchedules",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_sessions_ClassSchedules_schedule_id",
                table: "class_sessions",
                column: "schedule_id",
                principalTable: "ClassSchedules",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSchedules_classes_class_id",
                table: "ClassSchedules",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassStudents_classes_class_id",
                table: "ClassStudents",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassStudents_students_student_id",
                table: "ClassStudents",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
