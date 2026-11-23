using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanejadorCompras.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equalizations_Users_UserId",
                table: "Equalizations");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_UserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationRequests_Users_UserId",
                table: "QuotationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_Users_UserId",
                table: "ShoppingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Users_UserId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_UserId_Cnpj",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_UserId_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_QuotationRequests_Code",
                table: "QuotationRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuotationRequests_UserId_CreatedAtUtc",
                table: "QuotationRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Code",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_UserId_CreatedAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_UserId_SourceShoppingListId_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_Code",
                table: "Equalizations");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_UserId_CreatedAtUtc",
                table: "Equalizations");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_UserId_RequestId",
                table: "Equalizations");

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveOrganizationId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermsVersion",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Suppliers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ShoppingListSuppliers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ShoppingLists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ShoppingItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "QuotationRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "PurchaseOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ItemQuotes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Equalizations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => new { x.OrganizationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_Memberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Memberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationDocumentCounters",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationDocumentCounters", x => new { x.OrganizationId, x.Kind, x.Year });
                    table.ForeignKey(
                        name: "FK_OrganizationDocumentCounters_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "citext", maxLength: 320, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Data migration (hand-written). It must run after the new tables exist and
            // before the indexes and foreign keys on OrganizationId are created.
            //
            // Every existing user gets a personal organization and becomes its
            // administrator (Role = 1). The organization reuses the user's id, which
            // makes the mapping trivial to audit: data that belonged to user X now
            // belongs to organization X, whose only member is user X.
            migrationBuilder.Sql(
                """
                INSERT INTO "Organizations" ("Id", "Name", "CreatedAtUtc")
                SELECT u."Id",
                       left('Organização de ' || split_part(u."Email"::text, '@', 1), 120),
                       u."CreatedAt"
                FROM "Users" u;

                INSERT INTO "Memberships" ("OrganizationId", "UserId", "Role", "CreatedAtUtc")
                SELECT u."Id", u."Id", 1, u."CreatedAt"
                FROM "Users" u;

                UPDATE "ShoppingLists"     SET "OrganizationId" = "UserId";
                UPDATE "Suppliers"         SET "OrganizationId" = "UserId";
                UPDATE "QuotationRequests" SET "OrganizationId" = "UserId";
                UPDATE "Equalizations"     SET "OrganizationId" = "UserId";
                UPDATE "PurchaseOrders"    SET "OrganizationId" = "UserId";

                UPDATE "ShoppingItems" item
                SET "OrganizationId" = list."OrganizationId"
                FROM "ShoppingLists" list
                WHERE list."Id" = item."ShoppingListId";

                UPDATE "ShoppingListSuppliers" link
                SET "OrganizationId" = list."OrganizationId"
                FROM "ShoppingLists" list
                WHERE list."Id" = link."ShoppingListId";

                UPDATE "ItemQuotes" quote
                SET "OrganizationId" = item."OrganizationId"
                FROM "ShoppingItems" item
                WHERE item."Id" = quote."ShoppingItemId";
                """);

            // Abort the whole migration instead of leaving an orphan row or a price or
            // list link that would point to a supplier of another organization.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    orphans bigint;
                    crossed bigint;
                BEGIN
                    SELECT (SELECT count(*) FROM "ShoppingLists"         WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "ShoppingItems"         WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "ItemQuotes"            WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "Suppliers"             WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "ShoppingListSuppliers" WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "QuotationRequests"     WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "Equalizations"         WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                         + (SELECT count(*) FROM "PurchaseOrders"        WHERE "OrganizationId" = '00000000-0000-0000-0000-000000000000')
                    INTO orphans;

                    IF orphans > 0 THEN
                        RAISE EXCEPTION 'AddOrganizationsAndTeam: % row(s) were left without an organization.', orphans;
                    END IF;

                    SELECT (SELECT count(*)
                            FROM "ItemQuotes" quote
                            JOIN "Suppliers" supplier ON supplier."Id" = quote."SupplierId"
                            WHERE supplier."OrganizationId" <> quote."OrganizationId")
                         + (SELECT count(*)
                            FROM "ShoppingListSuppliers" link
                            JOIN "Suppliers" supplier ON supplier."Id" = link."SupplierId"
                            WHERE supplier."OrganizationId" <> link."OrganizationId")
                    INTO crossed;

                    IF crossed > 0 THEN
                        RAISE EXCEPTION 'AddOrganizationsAndTeam: % price(s) or list link(s) reference a supplier of another user.', crossed;
                    END IF;
                END $$;
                """);

            // New rows must always state their organization explicitly.
            foreach (var table in new[]
                     {
                         "ShoppingLists", "ShoppingItems", "ItemQuotes", "Suppliers",
                         "ShoppingListSuppliers", "QuotationRequests", "Equalizations", "PurchaseOrders"
                     })
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"OrganizationId\" DROP DEFAULT;");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveOrganizationId",
                table: "Users",
                column: "ActiveOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_OrganizationId_Cnpj",
                table: "Suppliers",
                columns: new[] { "OrganizationId", "Cnpj" },
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_OrganizationId_Name",
                table: "Suppliers",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UserId",
                table: "Suppliers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListSuppliers_OrganizationId",
                table: "ShoppingListSuppliers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingLists_OrganizationId",
                table: "ShoppingLists",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_OrganizationId",
                table: "ShoppingItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRequests_OrganizationId_Code",
                table: "QuotationRequests",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRequests_OrganizationId_CreatedAtUtc",
                table: "QuotationRequests",
                columns: new[] { "OrganizationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRequests_UserId",
                table: "QuotationRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrganizationId_Code",
                table: "PurchaseOrders",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrganizationId_CreatedAtUtc",
                table: "PurchaseOrders",
                columns: new[] { "OrganizationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrganizationId_SourceShoppingListId_Supplier~",
                table: "PurchaseOrders",
                columns: new[] { "OrganizationId", "SourceShoppingListId", "SupplierId" },
                unique: true,
                filter: "\"Status\" <> 3 AND \"SourceShoppingListId\" IS NOT NULL AND \"SupplierId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_UserId",
                table: "PurchaseOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemQuotes_OrganizationId",
                table: "ItemQuotes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_OrganizationId_Code",
                table: "Equalizations",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_OrganizationId_CreatedAtUtc",
                table: "Equalizations",
                columns: new[] { "OrganizationId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_OrganizationId_RequestId",
                table: "Equalizations",
                columns: new[] { "OrganizationId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_UserId",
                table: "Equalizations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InvitedByUserId",
                table: "OrganizationInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_OrganizationId_Email",
                table: "OrganizationInvitations",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_TokenHash",
                table: "OrganizationInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Equalizations_Organizations_OrganizationId",
                table: "Equalizations",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equalizations_Users_UserId",
                table: "Equalizations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ItemQuotes_Organizations_OrganizationId",
                table: "ItemQuotes",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Organizations_OrganizationId",
                table: "PurchaseOrders",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_UserId",
                table: "PurchaseOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationRequests_Organizations_OrganizationId",
                table: "QuotationRequests",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationRequests_Users_UserId",
                table: "QuotationRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingItems_Organizations_OrganizationId",
                table: "ShoppingItems",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_Organizations_OrganizationId",
                table: "ShoppingLists",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_Users_UserId",
                table: "ShoppingLists",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListSuppliers_Organizations_OrganizationId",
                table: "ShoppingListSuppliers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Organizations_OrganizationId",
                table: "Suppliers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Users_UserId",
                table: "Suppliers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_ActiveOrganizationId",
                table: "Users",
                column: "ActiveOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equalizations_Organizations_OrganizationId",
                table: "Equalizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Equalizations_Users_UserId",
                table: "Equalizations");

            migrationBuilder.DropForeignKey(
                name: "FK_ItemQuotes_Organizations_OrganizationId",
                table: "ItemQuotes");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Organizations_OrganizationId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Users_UserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationRequests_Organizations_OrganizationId",
                table: "QuotationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_QuotationRequests_Users_UserId",
                table: "QuotationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingItems_Organizations_OrganizationId",
                table: "ShoppingItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_Organizations_OrganizationId",
                table: "ShoppingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingLists_Users_UserId",
                table: "ShoppingLists");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListSuppliers_Organizations_OrganizationId",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Organizations_OrganizationId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Users_UserId",
                table: "Suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_ActiveOrganizationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "OrganizationDocumentCounters");

            migrationBuilder.DropTable(
                name: "OrganizationInvitations");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Users_ActiveOrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_OrganizationId_Cnpj",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_OrganizationId_Name",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_UserId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListSuppliers_OrganizationId",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingLists_OrganizationId",
                table: "ShoppingLists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingItems_OrganizationId",
                table: "ShoppingItems");

            migrationBuilder.DropIndex(
                name: "IX_QuotationRequests_OrganizationId_Code",
                table: "QuotationRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuotationRequests_OrganizationId_CreatedAtUtc",
                table: "QuotationRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuotationRequests_UserId",
                table: "QuotationRequests");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrganizationId_Code",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrganizationId_CreatedAtUtc",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrganizationId_SourceShoppingListId_Supplier~",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_UserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_ItemQuotes_OrganizationId",
                table: "ItemQuotes");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_OrganizationId_Code",
                table: "Equalizations");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_OrganizationId_CreatedAtUtc",
                table: "Equalizations");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_OrganizationId_RequestId",
                table: "Equalizations");

            migrationBuilder.DropIndex(
                name: "IX_Equalizations_UserId",
                table: "Equalizations");

            migrationBuilder.DropColumn(
                name: "ActiveOrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ShoppingListSuppliers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ShoppingLists");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ShoppingItems");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "QuotationRequests");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ItemQuotes");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Equalizations");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UserId_Cnpj",
                table: "Suppliers",
                columns: new[] { "UserId", "Cnpj" },
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_UserId_Name",
                table: "Suppliers",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRequests_Code",
                table: "QuotationRequests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotationRequests_UserId_CreatedAtUtc",
                table: "QuotationRequests",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Code",
                table: "PurchaseOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_UserId_CreatedAtUtc",
                table: "PurchaseOrders",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_UserId_SourceShoppingListId_SupplierId",
                table: "PurchaseOrders",
                columns: new[] { "UserId", "SourceShoppingListId", "SupplierId" },
                unique: true,
                filter: "\"Status\" <> 3 AND \"SourceShoppingListId\" IS NOT NULL AND \"SupplierId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_Code",
                table: "Equalizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_UserId_CreatedAtUtc",
                table: "Equalizations",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Equalizations_UserId_RequestId",
                table: "Equalizations",
                columns: new[] { "UserId", "RequestId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Equalizations_Users_UserId",
                table: "Equalizations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Users_UserId",
                table: "PurchaseOrders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuotationRequests_Users_UserId",
                table: "QuotationRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingLists_Users_UserId",
                table: "ShoppingLists",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Users_UserId",
                table: "Suppliers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
