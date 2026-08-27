using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FCG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRoleTableWithEnum : Migration
    {
        private const string AdministratorRoleId = "22222222-2222-2222-2222-222222222222";
        private const string UserRoleId = "11111111-1111-1111-1111-111111111111";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Nova coluna Role (nome do enum), temporariamente nullable para o backfill.
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // 2. Backfill a partir do RoleId (Guids determinísticos) antes de descartá-lo.
            migrationBuilder.Sql(
                $"""
                 UPDATE "Users"
                 SET "Role" = CASE
                     WHEN "RoleId" = '{AdministratorRoleId}' THEN 'Administrator'
                     ELSE 'User'
                 END;
                 """);

            // 3. Com os dados migrados, torna a coluna obrigatória.
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldNullable: true);

            // 4. Remove a antiga FK, índice e coluna RoleId.
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            // 5. Remove a tabela Roles.
            migrationBuilder.DropTable(
                name: "Roles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Recria a tabela Roles e o seed dos dois papéis.
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid(UserRoleId), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acesso à plataforma e biblioteca de jogos.", true, "Usuário", null },
                    { new Guid(AdministratorRoleId), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pode cadastrar jogos, administrar usuários e criar promoções.", true, "Administrador", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            // 2. Recria RoleId nullable para o backfill reverso.
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                $"""
                 UPDATE "Users"
                 SET "RoleId" = CASE
                     WHEN "Role" = 'Administrator' THEN '{AdministratorRoleId}'
                     ELSE '{UserRoleId}'
                 END::uuid;
                 """);

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 3. Recria índice e FK.
            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 4. Remove a coluna Role.
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
