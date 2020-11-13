using System.Collections.Generic;
using Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

namespace HappyTravel.Data.Migrations
{
    public partial class AddLocationsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    Coordinates = table.Column<Point>(type: "geography (point)", nullable: true),
                    Country = table.Column<string>(type: "jsonb", nullable: true),
                    Distance = table.Column<int>(nullable: false),
                    Locality = table.Column<string>(type: "jsonb", nullable: true),
                    Name = table.Column<string>(type: "jsonb", nullable: true),
                    Source = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    DataProviders = table.Column<List<Suppliers>>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
