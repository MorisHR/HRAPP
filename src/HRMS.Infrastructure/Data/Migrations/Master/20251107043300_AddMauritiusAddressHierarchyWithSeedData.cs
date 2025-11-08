using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddMauritiusAddressHierarchyWithSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Districts",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistrictCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DistrictName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DistrictNameFrench = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AreaSqKm = table.Column<decimal>(type: "numeric", nullable: true),
                    Population = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Villages",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VillageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VillageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VillageNameFrench = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    LocalityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Villages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Villages_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "master",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostalCodes",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VillageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DistrictName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VillageId = table.Column<int>(type: "integer", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    LocalityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostalCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostalCodes_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalSchema: "master",
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostalCodes_Villages_VillageId",
                        column: x => x.VillageId,
                        principalSchema: "master",
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Districts_DisplayOrder",
                schema: "master",
                table: "Districts",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_DistrictCode",
                schema: "master",
                table: "Districts",
                column: "DistrictCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_Region",
                schema: "master",
                table: "Districts",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_PostalCodes_Code",
                schema: "master",
                table: "PostalCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostalCodes_DistrictId",
                schema: "master",
                table: "PostalCodes",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalCodes_VillageId",
                schema: "master",
                table: "PostalCodes",
                column: "VillageId");

            migrationBuilder.CreateIndex(
                name: "IX_PostalCodes_VillageName_DistrictName",
                schema: "master",
                table: "PostalCodes",
                columns: new[] { "VillageName", "DistrictName" });

            migrationBuilder.CreateIndex(
                name: "IX_Villages_DisplayOrder",
                schema: "master",
                table: "Villages",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_DistrictId",
                schema: "master",
                table: "Villages",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_PostalCode",
                schema: "master",
                table: "Villages",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_Villages_VillageCode",
                schema: "master",
                table: "Villages",
                column: "VillageCode",
                unique: true);

            // =====================================================
            // SEED DATA: MAURITIUS DISTRICTS (9 Districts)
            // =====================================================
            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                schema: "master",
                table: "Districts",
                columns: new[] { "Id", "DistrictCode", "DistrictName", "DistrictNameFrench", "Region", "AreaSqKm", "Population", "DisplayOrder", "IsActive", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { 1, "BL", "Black River", "Rivière Noire", "West", 259m, 80000, 1, true, now, false },
                    { 2, "FL", "Flacq", "Flacq", "East", 298m, 138000, 2, true, now, false },
                    { 3, "GP", "Grand Port", "Grand Port", "South", 260m, 113000, 3, true, now, false },
                    { 4, "MO", "Moka", "Moka", "Central", 231m, 83000, 4, true, now, false },
                    { 5, "PA", "Pamplemousses", "Pamplemousses", "North", 179m, 140000, 5, true, now, false },
                    { 6, "PW", "Plaines Wilhems", "Plaines Wilhems", "Central", 203m, 380000, 6, true, now, false },
                    { 7, "PL", "Port Louis", "Port Louis", "West", 42m, 150000, 7, true, now, false },
                    { 8, "RR", "Rivière du Rempart", "Rivière du Rempart", "North", 147m, 109000, 8, true, now, false },
                    { 9, "SA", "Savanne", "Savanne", "South", 245m, 69000, 9, true, now, false }
                });

            // =====================================================
            // SEED DATA: MAJOR VILLAGES & TOWNS (29 Locations)
            // =====================================================
            migrationBuilder.InsertData(
                schema: "master",
                table: "Villages",
                columns: new[] { "Id", "VillageCode", "VillageName", "VillageNameFrench", "PostalCode", "DistrictId", "LocalityType", "DisplayOrder", "IsActive", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    // Port Louis District - Major Cities
                    { 1, "PLOU", "Port Louis", "Port Louis", "11302", 7, "City", 1, true, now, false },

                    // Plaines Wilhems District - Major Towns
                    { 2, "QB", "Quatre Bornes", "Quatre Bornes", "72426", 6, "Town", 2, true, now, false },
                    { 3, "CURE", "Curepipe", "Curepipe", "74504", 6, "City", 3, true, now, false },
                    { 4, "VAC", "Vacoas", "Vacoas", "73403", 6, "Town", 4, true, now, false },
                    { 5, "RH", "Rose Hill", "Rose Hill", "71259", 6, "Town", 5, true, now, false },
                    { 6, "BB", "Beau Bassin", "Beau-Bassin", "71504", 6, "Town", 6, true, now, false },
                    { 7, "FLOR", "Floreal", "Floréal", "74207", 6, "Town", 7, true, now, false },
                    { 8, "PHX", "Phoenix", "Phoenix", "73504", 6, "Town", 8, true, now, false },

                    // Pamplemousses District - North Region
                    { 9, "GB", "Grand Baie", "Grand Baie", "30515", 5, "Town", 9, true, now, false },
                    { 10, "GOOD", "Goodlands", "Goodlands", "51502", 5, "Village", 10, true, now, false },
                    { 11, "TRIC", "Triolet", "Triolet", "21618", 5, "Village", 11, true, now, false },

                    // Rivière du Rempart District - North
                    { 12, "CAP", "Cap Malheureux", "Cap Malheureux", "31708", 8, "Village", 12, true, now, false },
                    { 13, "POUD", "Poudre d'Or", "Poudre d'Or", "31707", 8, "Village", 13, true, now, false },

                    // Flacq District - East
                    { 14, "CB", "Centre de Flacq", "Centre de Flacq", "40901", 2, "Town", 14, true, now, false },
                    { 15, "QUATC", "Quatre Cocos", "Quatre Cocos", "41520", 2, "Village", 15, true, now, false },
                    { 16, "BRAM", "Bras d'Eau", "Bras d'Eau", "41605", 2, "Village", 16, true, now, false },

                    // Grand Port District - South
                    { 17, "MAH", "Mahebourg", "Mahébourg", "50802", 3, "Town", 17, true, now, false },
                    { 18, "BLEU", "Blue Bay", "Blue Bay", "51502", 3, "Village", 18, true, now, false },

                    // Savanne District - South
                    { 19, "SOU", "Souillac", "Souillac", "60805", 9, "Village", 19, true, now, false },
                    { 20, "CHA", "Chamarel", "Chamarel", "91001", 9, "Village", 20, true, now, false },

                    // Black River District - West
                    { 21, "TAM", "Tamarin", "Tamarin", "90601", 1, "Village", 21, true, now, false },
                    { 22, "FL", "Flic en Flac", "Flic en Flac", "90501", 1, "Village", 22, true, now, false },
                    { 23, "ALBION", "Albion", "Albion", "90306", 1, "Village", 23, true, now, false },

                    // Moka District - Central
                    { 24, "MOKA", "Moka", "Moka", "80402", 4, "Town", 24, true, now, false },
                    { 25, "DAGU", "Dagotière", "Dagotière", "80520", 4, "Village", 25, true, now, false },

                    // Additional Major Towns
                    { 26, "EBENE", "Ebene", "Ébène", "72201", 6, "City", 26, true, now, false },
                    { 27, "RED", "Reduit", "Réduit", "80835", 4, "Village", 27, true, now, false },
                    { 28, "PEREYBERE", "Pereybere", "Péreybère", "30546", 8, "Village", 28, true, now, false },
                    { 29, "BEL", "Belle Mare", "Belle Mare", "41601", 2, "Village", 29, true, now, false }
                });

            // =====================================================
            // SEED DATA: POSTAL CODES (29 Postal Codes)
            // =====================================================
            migrationBuilder.InsertData(
                schema: "master",
                table: "PostalCodes",
                columns: new[] { "Id", "Code", "VillageName", "DistrictName", "Region", "VillageId", "DistrictId", "LocalityType", "IsPrimary", "IsActive", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { 1, "11302", "Port Louis", "Port Louis", "West", 1, 7, "City", true, true, now, false },
                    { 2, "72426", "Quatre Bornes", "Plaines Wilhems", "Central", 2, 6, "Town", true, true, now, false },
                    { 3, "74504", "Curepipe", "Plaines Wilhems", "Central", 3, 6, "City", true, true, now, false },
                    { 4, "73403", "Vacoas", "Plaines Wilhems", "Central", 4, 6, "Town", true, true, now, false },
                    { 5, "71259", "Rose Hill", "Plaines Wilhems", "Central", 5, 6, "Town", true, true, now, false },
                    { 6, "71504", "Beau Bassin", "Plaines Wilhems", "Central", 6, 6, "Town", true, true, now, false },
                    { 7, "74207", "Floreal", "Plaines Wilhems", "Central", 7, 6, "Town", true, true, now, false },
                    { 8, "73504", "Phoenix", "Plaines Wilhems", "Central", 8, 6, "Town", true, true, now, false },
                    { 9, "30515", "Grand Baie", "Pamplemousses", "North", 9, 5, "Town", true, true, now, false },
                    { 10, "51502", "Goodlands", "Pamplemousses", "North", 10, 5, "Village", true, true, now, false },
                    { 11, "21618", "Triolet", "Pamplemousses", "North", 11, 5, "Village", true, true, now, false },
                    { 12, "31708", "Cap Malheureux", "Rivière du Rempart", "North", 12, 8, "Village", true, true, now, false },
                    { 13, "31707", "Poudre d'Or", "Rivière du Rempart", "North", 13, 8, "Village", true, true, now, false },
                    { 14, "40901", "Centre de Flacq", "Flacq", "East", 14, 2, "Town", true, true, now, false },
                    { 15, "41520", "Quatre Cocos", "Flacq", "East", 15, 2, "Village", true, true, now, false },
                    { 16, "41605", "Bras d'Eau", "Flacq", "East", 16, 2, "Village", true, true, now, false },
                    { 17, "50802", "Mahebourg", "Grand Port", "South", 17, 3, "Town", true, true, now, false },
                    { 18, "50803", "Blue Bay", "Grand Port", "South", 18, 3, "Village", true, true, now, false },
                    { 19, "60805", "Souillac", "Savanne", "South", 19, 9, "Village", true, true, now, false },
                    { 20, "91001", "Chamarel", "Savanne", "South", 20, 9, "Village", true, true, now, false },
                    { 21, "90601", "Tamarin", "Black River", "West", 21, 1, "Village", true, true, now, false },
                    { 22, "90501", "Flic en Flac", "Black River", "West", 22, 1, "Village", true, true, now, false },
                    { 23, "90306", "Albion", "Black River", "West", 23, 1, "Village", true, true, now, false },
                    { 24, "80402", "Moka", "Moka", "Central", 24, 4, "Town", true, true, now, false },
                    { 25, "80520", "Dagotière", "Moka", "Central", 25, 4, "Village", true, true, now, false },
                    { 26, "72201", "Ebene", "Plaines Wilhems", "Central", 26, 6, "City", true, true, now, false },
                    { 27, "80835", "Reduit", "Moka", "Central", 27, 4, "Village", true, true, now, false },
                    { 28, "30546", "Pereybere", "Rivière du Rempart", "North", 28, 8, "Village", true, true, now, false },
                    { 29, "41601", "Belle Mare", "Flacq", "East", 29, 2, "Village", true, true, now, false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostalCodes",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Villages",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Districts",
                schema: "master");
        }
    }
}
