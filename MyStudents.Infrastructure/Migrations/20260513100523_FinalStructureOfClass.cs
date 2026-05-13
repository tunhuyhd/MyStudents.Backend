using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyStudents.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalStructureOfClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_classes_teachers_teacher_id",
                table: "classes");

            migrationBuilder.DropTable(
                name: "teachers");

            migrationBuilder.AddColumn<int>(
                name: "category_of_class",
                table: "classes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "subject_id",
                table: "classes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subjects", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_classes_subject_id",
                table: "classes",
                column: "subject_id");

            migrationBuilder.AddForeignKey(
                name: "FK_classes_subjects_subject_id",
                table: "classes",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_classes_users_teacher_id",
                table: "classes",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_classes_subjects_subject_id",
                table: "classes");

            migrationBuilder.DropForeignKey(
                name: "FK_classes_users_teacher_id",
                table: "classes");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropIndex(
                name: "IX_classes_subject_id",
                table: "classes");

            migrationBuilder.DropColumn(
                name: "category_of_class",
                table: "classes");

            migrationBuilder.DropColumn(
                name: "subject_id",
                table: "classes");

            migrationBuilder.CreateTable(
                name: "teachers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teachers", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_classes_teachers_teacher_id",
                table: "classes",
                column: "teacher_id",
                principalTable: "teachers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
