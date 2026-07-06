using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeData.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SecurityId",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BrokerId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FundId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SecurityId1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StrategyId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Brokers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brokers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exchanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Managers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Managers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Security",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ticker = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExchangeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Security", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Security_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Brokers",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { 1, "*None*", "System Default Broker" });

            migrationBuilder.InsertData(
                table: "Exchanges",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { 1, "*None*", "System Default Exchange" });

            migrationBuilder.InsertData(
                table: "Funds",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { 1, "*None*", "System Default Fund" });

            migrationBuilder.InsertData(
                table: "Managers",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { 1, "*None*", "System Default Manager" });

            migrationBuilder.InsertData(
                table: "Strategies",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[] { 1, "*None*", "System Default Strategy" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BrokerId",
                table: "Orders",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FundId",
                table: "Orders",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ManagerId",
                table: "Orders",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SecurityId",
                table: "Orders",
                column: "SecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SecurityId1",
                table: "Orders",
                column: "SecurityId1");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StrategyId",
                table: "Orders",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_Id",
                table: "Brokers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_Id",
                table: "Exchanges",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Funds_Id",
                table: "Funds",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Id",
                table: "Managers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_ExchangeId",
                table: "Security",
                column: "ExchangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Security_Id",
                table: "Security",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_Id",
                table: "Strategies",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Brokers_BrokerId",
                table: "Orders",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Funds_FundId",
                table: "Orders",
                column: "FundId",
                principalTable: "Funds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Managers_ManagerId",
                table: "Orders",
                column: "ManagerId",
                principalTable: "Managers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Security_SecurityId",
                table: "Orders",
                column: "SecurityId",
                principalTable: "Security",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Security_SecurityId1",
                table: "Orders",
                column: "SecurityId1",
                principalTable: "Security",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Strategies_StrategyId",
                table: "Orders",
                column: "StrategyId",
                principalTable: "Strategies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Brokers_BrokerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Funds_FundId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Managers_ManagerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Security_SecurityId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Security_SecurityId1",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Strategies_StrategyId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Brokers");

            migrationBuilder.DropTable(
                name: "Funds");

            migrationBuilder.DropTable(
                name: "Managers");

            migrationBuilder.DropTable(
                name: "Security");

            migrationBuilder.DropTable(
                name: "Strategies");

            migrationBuilder.DropTable(
                name: "Exchanges");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BrokerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FundId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ManagerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SecurityId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SecurityId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StrategyId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FundId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SecurityId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StrategyId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
