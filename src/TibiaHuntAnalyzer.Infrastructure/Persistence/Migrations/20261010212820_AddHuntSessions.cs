using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntAnalyzer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHuntSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HuntSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HuntLocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionStart = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SessionEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    Statistics_Balance = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_Damage = table.Column<long>(type: "INTEGER", nullable: true),
                    Statistics_DamagePerHour = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_Healing = table.Column<long>(type: "INTEGER", nullable: true),
                    Statistics_HealingPerHour = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_Loot = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_Supplies = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_XpGain = table.Column<long>(type: "INTEGER", nullable: true),
                    Statistics_XpPerHour = table.Column<decimal>(type: "TEXT", nullable: true),
                    Statistics_RawXpGain = table.Column<long>(type: "INTEGER", nullable: true),
                    Statistics_RawXpPerHour = table.Column<decimal>(type: "TEXT", nullable: true),
                    InputFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalInput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntSessions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HuntSessions_HuntLocations_HuntLocationId",
                        column: x => x.HuntLocationId,
                        principalTable: "HuntLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KilledMonsters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HuntSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KilledMonsters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KilledMonsters_HuntSessions_HuntSessionId",
                        column: x => x.HuntSessionId,
                        principalTable: "HuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LootedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HuntSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootedItems_HuntSessions_HuntSessionId",
                        column: x => x.HuntSessionId,
                        principalTable: "HuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HuntSessions_CharacterId",
                table: "HuntSessions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntSessions_HuntLocationId",
                table: "HuntSessions",
                column: "HuntLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_KilledMonsters_HuntSessionId",
                table: "KilledMonsters",
                column: "HuntSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LootedItems_HuntSessionId",
                table: "LootedItems",
                column: "HuntSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KilledMonsters");

            migrationBuilder.DropTable(
                name: "LootedItems");

            migrationBuilder.DropTable(
                name: "HuntSessions");
        }
    }
}
