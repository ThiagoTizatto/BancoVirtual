using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovimentacoesFinanceiras.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class InicialPostgres : Migration
    {
        private static readonly string[] ColunasIndiceComposto = ["conta_id", "criado_em"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saldo_atual = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    versao_linha = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    chave_idempotencia = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_lancamentos_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contas_cliente_id",
                table: "contas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_chave_idempotencia",
                table: "lancamentos",
                column: "chave_idempotencia",
                unique: true,
                filter: "chave_idempotencia IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_conta_id_criado_em",
                table: "lancamentos",
                columns: ColunasIndiceComposto);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "lancamentos");

            migrationBuilder.DropTable(
                name: "contas");
        }
    }
}
