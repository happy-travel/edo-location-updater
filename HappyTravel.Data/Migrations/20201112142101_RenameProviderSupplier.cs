using System.Collections.Generic;
using Common.Models;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HappyTravel.Data.Migrations
{
    public partial class RenameProviderSupplier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("DataProviders",
                "Locations",
                "Suppliers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("Suppliers",
                "Locations",
                "DataProviders");
        }
    }
}
