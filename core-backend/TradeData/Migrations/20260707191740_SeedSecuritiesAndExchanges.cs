using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TradeData.Migrations
{
    /// <inheritdoc />
    public partial class SeedSecuritiesAndExchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Strategies_Id",
                table: "Strategies");

            migrationBuilder.DropIndex(
                name: "IX_Security_Id",
                table: "Security");

            migrationBuilder.DropIndex(
                name: "IX_Managers_Id",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Funds_Id",
                table: "Funds");

            migrationBuilder.DropIndex(
                name: "IX_Exchanges_Id",
                table: "Exchanges");

            migrationBuilder.DropIndex(
                name: "IX_Brokers_Id",
                table: "Brokers");

            migrationBuilder.InsertData(
                table: "Exchanges",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { 2, "NASDAQ", "Nasdaq Stock Market" },
                    { 3, "NYSE", "New York Stock Exchange" }
                });

            migrationBuilder.InsertData(
                table: "Security",
                columns: new[] { "Id", "Description", "ExchangeId", "Ticker" },
                values: new object[,]
                {
                    { 1, "Apple Inc.", 2, "AAPL" },
                    { 2, "Microsoft Corp.", 2, "MSFT" },
                    { 3, "Amazon.com Inc.", 2, "AMZN" },
                    { 4, "Alphabet Inc.", 2, "GOOGL" },
                    { 5, "Meta Platforms Inc.", 2, "META" },
                    { 6, "NVIDIA Corp.", 2, "NVDA" },
                    { 7, "Tesla Inc.", 2, "TSLA" },
                    { 8, "Netflix Inc.", 2, "NFLX" },
                    { 9, "JPMorgan Chase & Co.", 3, "JPM" },
                    { 10, "Visa Inc.", 3, "V" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Security_Ticker",
                table: "Security",
                column: "Ticker",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Security_Ticker",
                table: "Security");

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Security",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Exchanges",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Exchanges",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_Id",
                table: "Strategies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Security_Id",
                table: "Security",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Id",
                table: "Managers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Funds_Id",
                table: "Funds",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_Id",
                table: "Exchanges",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_Id",
                table: "Brokers",
                column: "Id");
        }
    }
}
