using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanejadorCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierQuotationsAndCommercialTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TermsDeliveryDays",
                table: "ShoppingListSuppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TermsFreightCost",
                table: "ShoppingListSuppliers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TermsMinimumOrderValue",
                table: "ShoppingListSuppliers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsNotes",
                table: "ShoppingListSuppliers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsPaymentTerms",
                table: "ShoppingListSuppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TermsProposalValidUntil",
                table: "ShoppingListSuppliers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsUpdatedAtUtc",
                table: "ShoppingListSuppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupplierQuotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupplierEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    SupplierPhone = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmailSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ViewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermsDeliveryDays = table.Column<int>(type: "integer", nullable: true),
                    TermsFreightCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TermsMinimumOrderValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TermsNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TermsPaymentTerms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TermsProposalValidUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotations_QuotationRequests_QuotationRequestId",
                        column: x => x.QuotationRequestId,
                        principalTable: "QuotationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotationResponseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationRequestItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierQuotationResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationResponseItems_QuotationRequestItems_Quotat~",
                        column: x => x.QuotationRequestItemId,
                        principalTable: "QuotationRequestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierQuotationResponseItems_SupplierQuotations_SupplierQ~",
                        column: x => x.SupplierQuotationId,
                        principalTable: "SupplierQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationResponseItems_QuotationRequestItemId",
                table: "SupplierQuotationResponseItems",
                column: "QuotationRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotationResponseItems_SupplierQuotationId_Quotatio~",
                table: "SupplierQuotationResponseItems",
                columns: new[] { "SupplierQuotationId", "QuotationRequestItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_QuotationRequestId_SupplierId",
                table: "SupplierQuotations",
                columns: new[] { "QuotationRequestId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_SupplierId",
                table: "SupplierQuotations",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotations_TokenHash",
                table: "SupplierQuotations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierQuotationResponseItems");

            migrationBuilder.DropTable(
                name: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "TermsDeliveryDays",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsFreightCost",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsMinimumOrderValue",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsNotes",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsPaymentTerms",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsProposalValidUntil",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "TermsUpdatedAtUtc",
                table: "ShoppingListSuppliers");
        }
    }
}
