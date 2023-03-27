using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spyglass.Core.Migrations
{
    public partial class AuthenticatedRequestData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authenticated_requests",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    server_name = table.Column<string>(type: "text", nullable: true),
                    request_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authenticated_requests", x => new { x.client_id, x.ip_address });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authenticated_requests");
        }
    }
}
