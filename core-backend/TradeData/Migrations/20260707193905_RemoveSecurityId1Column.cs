using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSecurityId1Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Security_SecurityId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SecurityId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SecurityId1",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SecurityId1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SecurityId1",
                table: "Orders",
                column: "SecurityId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Security_SecurityId1",
                table: "Orders",
                column: "SecurityId1",
                principalTable: "Security",
                principalColumn: "Id");
        }
    }
}
