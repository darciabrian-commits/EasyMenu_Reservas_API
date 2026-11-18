using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyMenu.Reservas.API.Migrations
{
    public partial class ReservaDisponibilidadActiva : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Reservas]
                    WHERE DATALENGTH([Estado]) > 40
                )
                    THROW 51000, N'Migración cancelada: existe un estado de reserva de más de 20 caracteres. Revise los datos antes de continuar.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [Reservas]
                    WHERE [Estado] <> N'CANCELADA'
                    GROUP BY [MesaId], [FechaReserva], [HoraReserva]
                    HAVING COUNT(*) > 1
                )
                    THROW 51001, N'Migración cancelada: existen reservas activas duplicadas para una mesa, fecha y hora. Resuelva los registros conservando los datos antes de continuar.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Reservas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_MesaId_FechaReserva_HoraReserva",
                table: "Reservas",
                columns: new[] { "MesaId", "FechaReserva", "HoraReserva" },
                unique: true,
                filter: "[Estado] <> N'CANCELADA'");

            migrationBuilder.DropIndex(
                name: "IX_Reservas_MesaId",
                table: "Reservas");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_MesaId_FechaReserva_HoraReserva",
                table: "Reservas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_MesaId",
                table: "Reservas",
                column: "MesaId");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Reservas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
