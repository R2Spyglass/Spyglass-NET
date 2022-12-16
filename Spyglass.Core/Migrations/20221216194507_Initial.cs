using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spyglass.Core.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "PlayerSanctionIds");

            migrationBuilder.CreateTable(
                name: "maintainer_identities",
                columns: table => new
                {
                    unique_id = table.Column<string>(type: "text", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintainer_identities", x => x.unique_id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    unique_id = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    is_maintainer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_seen_on_server = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.unique_id);
                });

            migrationBuilder.CreateTable(
                name: "player_aliases",
                columns: table => new
                {
                    unique_id = table.Column<string>(type: "text", nullable: false),
                    alias = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_aliases", x => new { x.unique_id, x.alias });
                    table.ForeignKey(
                        name: "fk_player_aliases_players_owning_player_temp_id",
                        column: x => x.unique_id,
                        principalTable: "players",
                        principalColumn: "unique_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sanctions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"PlayerSanctionIds\"')"),
                    unique_id = table.Column<string>(type: "text", nullable: false),
                    issuer_id = table.Column<string>(type: "text", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    punishment_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sanctions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sanctions_players_issuer_info_unique_id",
                        column: x => x.issuer_id,
                        principalTable: "players",
                        principalColumn: "unique_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sanctions_players_owning_player_unique_id",
                        column: x => x.unique_id,
                        principalTable: "players",
                        principalColumn: "unique_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sanctions_issuer_id",
                table: "sanctions",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_sanctions_unique_id",
                table: "sanctions",
                column: "unique_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintainer_identities");

            migrationBuilder.DropTable(
                name: "player_aliases");

            migrationBuilder.DropTable(
                name: "sanctions");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropSequence(
                name: "PlayerSanctionIds");
        }
    }
}
