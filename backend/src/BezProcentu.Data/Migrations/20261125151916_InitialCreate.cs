using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BezProcentu.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "collection_runs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreSlug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false),
                    PagesFailed = table.Column<int>(type: "integer", nullable: false),
                    ProductsSeen = table.Column<int>(type: "integer", nullable: false),
                    OffersActive = table.Column<int>(type: "integer", nullable: false),
                    StorePolicyActive = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collection_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreSlug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StoreProductId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CategorySlug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsOfferActive = table.Column<bool>(type: "boolean", nullable: false),
                    OfferScope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InterestRatePercent = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    MaxTermMonths = table.Column<int>(type: "integer", nullable: false),
                    TermsMonths = table.Column<int[]>(type: "integer[]", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    MaxAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    MonthlyPayment = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Conditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OfferSourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OfferVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_collection_runs_StoreSlug_StartedAt",
                table: "collection_runs",
                columns: new[] { "StoreSlug", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_products_IsOfferActive_OfferVerifiedAt_CategorySlug",
                table: "products",
                columns: new[] { "IsOfferActive", "OfferVerifiedAt", "CategorySlug" });

            migrationBuilder.CreateIndex(
                name: "IX_products_StoreSlug_StoreProductId",
                table: "products",
                columns: new[] { "StoreSlug", "StoreProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collection_runs");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
