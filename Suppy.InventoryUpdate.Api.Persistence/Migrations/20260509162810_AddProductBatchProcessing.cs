using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suppy.InventoryUpdate.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBatchProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    LastBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastUpdatedFromBatchAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
                    table.CheckConstraint("CK_Products_Stock_NonNegative", "\"Stock\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ProductUpdateBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ProcessedItems = table.Column<int>(type: "integer", nullable: false),
                    FailedItems = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUpdateBatches", x => x.Id);
                    table.CheckConstraint("CK_ProductUpdateBatches_FailedItems_NonNegative", "\"FailedItems\" >= 0");
                    table.CheckConstraint("CK_ProductUpdateBatches_ProcessedItems_NonNegative", "\"ProcessedItems\" >= 0");
                    table.CheckConstraint("CK_ProductUpdateBatches_TotalItems_Positive", "\"TotalItems\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ProductUpdateBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUpdateBatchItems", x => x.Id);
                    table.CheckConstraint("CK_ProductUpdateBatchItems_Price_NonNegative", "\"Price\" >= 0");
                    table.CheckConstraint("CK_ProductUpdateBatchItems_Stock_NonNegative", "\"Stock\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductUpdateBatchItems_ProductUpdateBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductUpdateBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAtUtc_Id",
                table: "Products",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CreatedAtUtc_Id",
                table: "Products",
                columns: new[] { "TenantId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_ItemId",
                table: "Products",
                columns: new[] { "TenantId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_LastUpdatedFromBatchAtUtc",
                table: "Products",
                columns: new[] { "TenantId", "LastUpdatedFromBatchAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_CreatedAtUtc_Id",
                table: "ProductUpdateBatches",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_IsDeleted",
                table: "ProductUpdateBatches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_TenantId",
                table: "ProductUpdateBatches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_TenantId_CreatedAtUtc_Id",
                table: "ProductUpdateBatches",
                columns: new[] { "TenantId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_TenantId_IdempotencyKey",
                table: "ProductUpdateBatches",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatches_TenantId_Status_CreatedAtUtc",
                table: "ProductUpdateBatches",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_BatchId_ItemId",
                table: "ProductUpdateBatchItems",
                columns: new[] { "BatchId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_CreatedAtUtc_Id",
                table: "ProductUpdateBatchItems",
                columns: new[] { "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_IsDeleted",
                table: "ProductUpdateBatchItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_TenantId",
                table: "ProductUpdateBatchItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_TenantId_BatchId",
                table: "ProductUpdateBatchItems",
                columns: new[] { "TenantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_TenantId_ItemId",
                table: "ProductUpdateBatchItems",
                columns: new[] { "TenantId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateBatchItems_TenantId_Status",
                table: "ProductUpdateBatchItems",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ProductUpdateBatchItems");

            migrationBuilder.DropTable(
                name: "ProductUpdateBatches");
        }
    }
}
