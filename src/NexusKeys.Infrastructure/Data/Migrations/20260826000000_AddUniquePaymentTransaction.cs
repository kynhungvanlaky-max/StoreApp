using GameStore.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.API.Migrations;

[DbContext(typeof(GameStoreDbContext))]
[Migration("20260826000000_AddUniquePaymentTransaction")]
public partial class AddUniquePaymentTransaction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PaymentGateway",
            table: "Transactions",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "GatewayTransactionId",
            table: "Transactions",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_PaymentGateway_GatewayTransactionId",
            table: "Transactions",
            columns: new[] { "PaymentGateway", "GatewayTransactionId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Transactions_PaymentGateway_GatewayTransactionId",
            table: "Transactions");

        migrationBuilder.AlterColumn<string>(
            name: "PaymentGateway",
            table: "Transactions",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(50)",
            oldMaxLength: 50);

        migrationBuilder.AlterColumn<string>(
            name: "GatewayTransactionId",
            table: "Transactions",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);
    }
}
