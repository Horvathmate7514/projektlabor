using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeltarKezelo.Szerver.Adat.Migraciok
{
    /// <inheritdoc />
    public partial class Kezdeti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EszkozAllapot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nev = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Megszunt = table.Column<bool>(type: "bit", nullable: false),
                    Sorrend = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EszkozAllapot", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EszkozTipus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nev = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SzuloId = table.Column<long>(type: "bigint", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EszkozTipus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EszkozTipus_EszkozTipus_SzuloId",
                        column: x => x.SzuloId,
                        principalTable: "EszkozTipus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Felhasznalo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Felhasznalonev = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nev = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JelszoHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SzervezetiEgyseg = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false),
                    UtolsoBelepes = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Felhasznalo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Helyiseg",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Epulet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Emelet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Szobaszam = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Megnevezes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Vonalkod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Helyiseg", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KodTipus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nev = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VonalkodkentHasznalhato = table.Column<bool>(type: "bit", nullable: false),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KodTipus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leltarkorzet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nev = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SzervezetiEgyseg = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leltarkorzet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Szerepkor",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nev = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Szerepkor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditNaplo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Entitas = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntitasId = table.Column<long>(type: "bigint", nullable: false),
                    Muvelet = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    RegiErtekJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UjErtekJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: true),
                    Idopont = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditNaplo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditNaplo_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelelosSzemely",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nev = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Azonosito = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SzervezetiEgyseg = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelelosSzemely", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FelelosSzemely_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportFutas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fajlnev = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Idopont = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: false),
                    SorokSzama = table.Column<int>(type: "int", nullable: false),
                    Sikeres = table.Column<int>(type: "int", nullable: false),
                    Hibas = table.Column<int>(type: "int", nullable: false),
                    Statusz = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFutas", x => x.Id);
                    table.CheckConstraint("CK_ImportFutas_Statusz", "[Statusz] IN ('FOLYAMATBAN', 'SIKERES', 'RESZLEGES', 'SIKERTELEN')");
                    table.ForeignKey(
                        name: "FK_ImportFutas_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeltarIdoszak",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Megnevezes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Kezdete = table.Column<DateOnly>(type: "date", nullable: false),
                    Vege = table.Column<DateOnly>(type: "date", nullable: true),
                    Statusz = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Lezarva = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LezartaId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeltarIdoszak", x => x.Id);
                    table.CheckConstraint("CK_LeltarIdoszak_Idoszak", "[Vege] IS NULL OR [Vege] >= [Kezdete]");
                    table.CheckConstraint("CK_LeltarIdoszak_Lezaras", "[Statusz] <> 'LEZART' OR [Lezarva] IS NOT NULL");
                    table.CheckConstraint("CK_LeltarIdoszak_Statusz", "[Statusz] IN ('TERVEZETT', 'NYITOTT', 'LEZART')");
                    table.CheckConstraint("CK_LeltarIdoszak_Tipus", "[Tipus] IN ('EVES', 'RENDKIVULI')");
                    table.ForeignKey(
                        name: "FK_LeltarIdoszak_Felhasznalo_LezartaId",
                        column: x => x.LezartaId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Eszkoz",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Megnevezes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EszkozTipusId = table.Column<long>(type: "bigint", nullable: true),
                    LeltarkorzetId = table.Column<long>(type: "bigint", nullable: false),
                    ElvartMennyiseg = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MennyisegiEgyseg = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Ertek = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BeszerzesDatuma = table.Column<DateOnly>(type: "date", nullable: true),
                    EszkozAllapotId = table.Column<long>(type: "bigint", nullable: false),
                    Megjegyzes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Letrehozva = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LetrehoztaId = table.Column<long>(type: "bigint", nullable: true),
                    Modositva = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModositottaId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eszkoz", x => x.Id);
                    table.CheckConstraint("CK_Eszkoz_ElvartMennyiseg", "[ElvartMennyiseg] > 0");
                    table.ForeignKey(
                        name: "FK_Eszkoz_EszkozAllapot_EszkozAllapotId",
                        column: x => x.EszkozAllapotId,
                        principalTable: "EszkozAllapot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eszkoz_EszkozTipus_EszkozTipusId",
                        column: x => x.EszkozTipusId,
                        principalTable: "EszkozTipus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Eszkoz_Leltarkorzet_LeltarkorzetId",
                        column: x => x.LeltarkorzetId,
                        principalTable: "Leltarkorzet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelhasznaloLeltarkorzet",
                columns: table => new
                {
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: false),
                    JogosultKorzetekId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelhasznaloLeltarkorzet", x => new { x.FelhasznaloId, x.JogosultKorzetekId });
                    table.ForeignKey(
                        name: "FK_FelhasznaloLeltarkorzet_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelhasznaloLeltarkorzet_Leltarkorzet_JogosultKorzetekId",
                        column: x => x.JogosultKorzetekId,
                        principalTable: "Leltarkorzet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelhasznaloSzerepkor",
                columns: table => new
                {
                    FelhasznalokId = table.Column<long>(type: "bigint", nullable: false),
                    SzerepkorokId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelhasznaloSzerepkor", x => new { x.FelhasznalokId, x.SzerepkorokId });
                    table.ForeignKey(
                        name: "FK_FelhasznaloSzerepkor_Felhasznalo_FelhasznalokId",
                        column: x => x.FelhasznalokId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelhasznaloSzerepkor_Szerepkor_SzerepkorokId",
                        column: x => x.SzerepkorokId,
                        principalTable: "Szerepkor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportHiba",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportFutasId = table.Column<long>(type: "bigint", nullable: false),
                    Sor = table.Column<int>(type: "int", nullable: false),
                    Oszlop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Uzenet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ertek = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportHiba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportHiba_ImportFutas_ImportFutasId",
                        column: x => x.ImportFutasId,
                        principalTable: "ImportFutas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AllapotValtozas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EszkozId = table.Column<long>(type: "bigint", nullable: false),
                    RegiAllapotId = table.Column<long>(type: "bigint", nullable: true),
                    UjAllapotId = table.Column<long>(type: "bigint", nullable: false),
                    Indoklas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ugyiratszam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: false),
                    Idopont = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllapotValtozas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllapotValtozas_EszkozAllapot_RegiAllapotId",
                        column: x => x.RegiAllapotId,
                        principalTable: "EszkozAllapot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllapotValtozas_EszkozAllapot_UjAllapotId",
                        column: x => x.UjAllapotId,
                        principalTable: "EszkozAllapot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllapotValtozas_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllapotValtozas_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElvartTetel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeltarIdoszakId = table.Column<long>(type: "bigint", nullable: false),
                    EszkozId = table.Column<long>(type: "bigint", nullable: false),
                    LeltarkorzetId = table.Column<long>(type: "bigint", nullable: false),
                    ElvartMennyiseg = table.Column<int>(type: "int", nullable: false),
                    FelvitelOka = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElvartTetel", x => x.Id);
                    table.CheckConstraint("CK_ElvartTetel_ElvartMennyiseg", "[ElvartMennyiseg] > 0");
                    table.CheckConstraint("CK_ElvartTetel_FelvitelOka", "[FelvitelOka] IN ('PILLANATKEP', 'UTOLAGOS')");
                    table.ForeignKey(
                        name: "FK_ElvartTetel_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElvartTetel_LeltarIdoszak_LeltarIdoszakId",
                        column: x => x.LeltarIdoszakId,
                        principalTable: "LeltarIdoszak",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElvartTetel_Leltarkorzet_LeltarkorzetId",
                        column: x => x.LeltarkorzetId,
                        principalTable: "Leltarkorzet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EszkozKod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EszkozId = table.Column<long>(type: "bigint", nullable: false),
                    KodTipusId = table.Column<long>(type: "bigint", nullable: false),
                    Ertek = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ErtekNorm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, computedColumnSql: "UPPER(LTRIM(RTRIM([Ertek])))", stored: true),
                    Aktiv = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EszkozKod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EszkozKod_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EszkozKod_KodTipus_KodTipusId",
                        column: x => x.KodTipusId,
                        principalTable: "KodTipus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelelosHozzarendeles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EszkozId = table.Column<long>(type: "bigint", nullable: false),
                    FelelosId = table.Column<long>(type: "bigint", nullable: false),
                    ErvenyesTol = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ErvenyesIg = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelelosHozzarendeles", x => x.Id);
                    table.CheckConstraint("CK_FelelosHozzarendeles_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]");
                    table.ForeignKey(
                        name: "FK_FelelosHozzarendeles_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelelosHozzarendeles_FelelosSzemely_FelelosId",
                        column: x => x.FelelosId,
                        principalTable: "FelelosSzemely",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kiegeszito",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoEszkozId = table.Column<long>(type: "bigint", nullable: false),
                    KiegeszitoEszkozId = table.Column<long>(type: "bigint", nullable: true),
                    Leiras = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mennyiseg = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ErvenyesTol = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ErvenyesIg = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kiegeszito", x => x.Id);
                    table.CheckConstraint("CK_Kiegeszito_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]");
                    table.CheckConstraint("CK_Kiegeszito_Mennyiseg", "[Mennyiseg] > 0");
                    table.CheckConstraint("CK_Kiegeszito_NemOnmaga", "[KiegeszitoEszkozId] IS NULL OR [KiegeszitoEszkozId] <> [FoEszkozId]");
                    table.CheckConstraint("CK_Kiegeszito_Tartalom", "[KiegeszitoEszkozId] IS NOT NULL OR [Leiras] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Kiegeszito_Eszkoz_FoEszkozId",
                        column: x => x.FoEszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kiegeszito_Eszkoz_KiegeszitoEszkozId",
                        column: x => x.KiegeszitoEszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Leolvasas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeltarIdoszakId = table.Column<long>(type: "bigint", nullable: false),
                    BeolvasottKod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EszkozKodId = table.Column<long>(type: "bigint", nullable: true),
                    EszkozId = table.Column<long>(type: "bigint", nullable: true),
                    AktivKorzetId = table.Column<long>(type: "bigint", nullable: false),
                    HelyisegId = table.Column<long>(type: "bigint", nullable: true),
                    Mennyiseg = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Minosites = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    BeviteliMod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FelhasznaloId = table.Column<long>(type: "bigint", nullable: false),
                    Idopont = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Sztornozva = table.Column<bool>(type: "bit", nullable: false),
                    SztornoIndoklas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SztornoIdopont = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    SztornoztaId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leolvasas", x => x.Id);
                    table.CheckConstraint("CK_Leolvasas_BeviteliMod", "[BeviteliMod] IN ('OLVASO', 'KAMERA', 'BEILLESZTES', 'KEZI', 'DEMO')");
                    table.CheckConstraint("CK_Leolvasas_Eszkoz", "([Minosites] = 'ISMERETLEN_KOD' AND [EszkozId] IS NULL) OR ([Minosites] <> 'ISMERETLEN_KOD' AND [EszkozId] IS NOT NULL)");
                    table.CheckConstraint("CK_Leolvasas_Mennyiseg", "[Mennyiseg] > 0");
                    table.CheckConstraint("CK_Leolvasas_Minosites", "[Minosites] IN ('OK', 'MAS_KORZET', 'ISMETELT', 'TOBBLET', 'ISMERETLEN_KOD', 'NEM_AKTIV')");
                    table.CheckConstraint("CK_Leolvasas_Sztorno", "[Sztornozva] = 0 OR ([SztornoIndoklas] IS NOT NULL AND [SztornoIdopont] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Leolvasas_EszkozKod_EszkozKodId",
                        column: x => x.EszkozKodId,
                        principalTable: "EszkozKod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_Felhasznalo_FelhasznaloId",
                        column: x => x.FelhasznaloId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_Felhasznalo_SztornoztaId",
                        column: x => x.SztornoztaId,
                        principalTable: "Felhasznalo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_Helyiseg_HelyisegId",
                        column: x => x.HelyisegId,
                        principalTable: "Helyiseg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_LeltarIdoszak_LeltarIdoszakId",
                        column: x => x.LeltarIdoszakId,
                        principalTable: "LeltarIdoszak",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leolvasas_Leltarkorzet_AktivKorzetId",
                        column: x => x.AktivKorzetId,
                        principalTable: "Leltarkorzet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Elhelyezes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EszkozId = table.Column<long>(type: "bigint", nullable: false),
                    HelyisegId = table.Column<long>(type: "bigint", nullable: false),
                    ErvenyesTol = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ErvenyesIg = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Forras = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    LeolvasasId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elhelyezes", x => x.Id);
                    table.CheckConstraint("CK_Elhelyezes_Ervenyesseg", "[ErvenyesIg] IS NULL OR [ErvenyesIg] > [ErvenyesTol]");
                    table.CheckConstraint("CK_Elhelyezes_Forras", "[Forras] IN ('LELTAR', 'KEZI')");
                    table.ForeignKey(
                        name: "FK_Elhelyezes_Eszkoz_EszkozId",
                        column: x => x.EszkozId,
                        principalTable: "Eszkoz",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Elhelyezes_Helyiseg_HelyisegId",
                        column: x => x.HelyisegId,
                        principalTable: "Helyiseg",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Elhelyezes_Leolvasas_LeolvasasId",
                        column: x => x.LeolvasasId,
                        principalTable: "Leolvasas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiegeszitoEllenorzes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeolvasasId = table.Column<long>(type: "bigint", nullable: false),
                    KiegeszitoId = table.Column<long>(type: "bigint", nullable: false),
                    Mod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiegeszitoEllenorzes", x => x.Id);
                    table.CheckConstraint("CK_KiegeszitoEllenorzes_Mod", "[Mod] IN ('KOZVETLEN', 'FELTETELEZETT')");
                    table.ForeignKey(
                        name: "FK_KiegeszitoEllenorzes_Kiegeszito_KiegeszitoId",
                        column: x => x.KiegeszitoId,
                        principalTable: "Kiegeszito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KiegeszitoEllenorzes_Leolvasas_LeolvasasId",
                        column: x => x.LeolvasasId,
                        principalTable: "Leolvasas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EszkozAllapot",
                columns: new[] { "Id", "Megszunt", "Nev", "Sorrend" },
                values: new object[,]
                {
                    { 1L, false, "aktív", 1 },
                    { 2L, false, "javítás alatt", 2 },
                    { 3L, false, "kölcsönadva", 3 },
                    { 4L, false, "selejtezésre javasolt", 4 },
                    { 5L, true, "selejtezett", 5 },
                    { 6L, true, "elveszett", 6 },
                    { 7L, true, "ellopott", 7 },
                    { 8L, true, "átadva", 8 }
                });

            migrationBuilder.InsertData(
                table: "KodTipus",
                columns: new[] { "Id", "Aktiv", "Nev", "VonalkodkentHasznalhato" },
                values: new object[,]
                {
                    { 1L, true, "SAP szám", true },
                    { 2L, true, "Leltári szám 1", true },
                    { 3L, true, "Leltári szám 2", true },
                    { 4L, true, "Gyártási szám", true },
                    { 5L, true, "Belső vonalkód", true }
                });

            migrationBuilder.InsertData(
                table: "Szerepkor",
                columns: new[] { "Id", "Nev" },
                values: new object[,]
                {
                    { 1L, "Admin" },
                    { 2L, "Leltarfelelos" },
                    { 3L, "Leltarozo" },
                    { 4L, "Megtekinto" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllapotValtozas_EszkozId_Idopont",
                table: "AllapotValtozas",
                columns: new[] { "EszkozId", "Idopont" });

            migrationBuilder.CreateIndex(
                name: "IX_AllapotValtozas_FelhasznaloId",
                table: "AllapotValtozas",
                column: "FelhasznaloId");

            migrationBuilder.CreateIndex(
                name: "IX_AllapotValtozas_RegiAllapotId",
                table: "AllapotValtozas",
                column: "RegiAllapotId");

            migrationBuilder.CreateIndex(
                name: "IX_AllapotValtozas_UjAllapotId",
                table: "AllapotValtozas",
                column: "UjAllapotId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditNaplo_Entitas_EntitasId",
                table: "AuditNaplo",
                columns: new[] { "Entitas", "EntitasId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditNaplo_FelhasznaloId",
                table: "AuditNaplo",
                column: "FelhasznaloId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditNaplo_Idopont",
                table: "AuditNaplo",
                column: "Idopont");

            migrationBuilder.CreateIndex(
                name: "IX_Elhelyezes_EszkozId_Aktualis",
                table: "Elhelyezes",
                column: "EszkozId",
                unique: true,
                filter: "[ErvenyesIg] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Elhelyezes_EszkozId_ErvenyesTol",
                table: "Elhelyezes",
                columns: new[] { "EszkozId", "ErvenyesTol" });

            migrationBuilder.CreateIndex(
                name: "IX_Elhelyezes_HelyisegId",
                table: "Elhelyezes",
                column: "HelyisegId");

            migrationBuilder.CreateIndex(
                name: "IX_Elhelyezes_LeolvasasId",
                table: "Elhelyezes",
                column: "LeolvasasId");

            migrationBuilder.CreateIndex(
                name: "IX_ElvartTetel_EszkozId",
                table: "ElvartTetel",
                column: "EszkozId");

            migrationBuilder.CreateIndex(
                name: "IX_ElvartTetel_LeltarIdoszakId_EszkozId",
                table: "ElvartTetel",
                columns: new[] { "LeltarIdoszakId", "EszkozId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElvartTetel_LeltarkorzetId",
                table: "ElvartTetel",
                column: "LeltarkorzetId");

            migrationBuilder.CreateIndex(
                name: "IX_Eszkoz_EszkozAllapotId",
                table: "Eszkoz",
                column: "EszkozAllapotId");

            migrationBuilder.CreateIndex(
                name: "IX_Eszkoz_EszkozTipusId",
                table: "Eszkoz",
                column: "EszkozTipusId");

            migrationBuilder.CreateIndex(
                name: "IX_Eszkoz_LeltarkorzetId",
                table: "Eszkoz",
                column: "LeltarkorzetId");

            migrationBuilder.CreateIndex(
                name: "IX_Eszkoz_Megnevezes",
                table: "Eszkoz",
                column: "Megnevezes");

            migrationBuilder.CreateIndex(
                name: "IX_EszkozAllapot_Nev",
                table: "EszkozAllapot",
                column: "Nev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EszkozKod_ErtekNorm",
                table: "EszkozKod",
                column: "ErtekNorm",
                unique: true,
                filter: "[Aktiv] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EszkozKod_EszkozId",
                table: "EszkozKod",
                column: "EszkozId");

            migrationBuilder.CreateIndex(
                name: "IX_EszkozKod_KodTipusId",
                table: "EszkozKod",
                column: "KodTipusId");

            migrationBuilder.CreateIndex(
                name: "IX_EszkozTipus_SzuloId",
                table: "EszkozTipus",
                column: "SzuloId");

            migrationBuilder.CreateIndex(
                name: "IX_FelelosHozzarendeles_EszkozId_Aktualis",
                table: "FelelosHozzarendeles",
                column: "EszkozId",
                unique: true,
                filter: "[ErvenyesIg] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FelelosHozzarendeles_EszkozId_ErvenyesTol",
                table: "FelelosHozzarendeles",
                columns: new[] { "EszkozId", "ErvenyesTol" });

            migrationBuilder.CreateIndex(
                name: "IX_FelelosHozzarendeles_FelelosId",
                table: "FelelosHozzarendeles",
                column: "FelelosId");

            migrationBuilder.CreateIndex(
                name: "IX_FelelosSzemely_Azonosito",
                table: "FelelosSzemely",
                column: "Azonosito",
                unique: true,
                filter: "[Azonosito] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FelelosSzemely_FelhasznaloId",
                table: "FelelosSzemely",
                column: "FelhasznaloId",
                unique: true,
                filter: "[FelhasznaloId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FelelosSzemely_Nev",
                table: "FelelosSzemely",
                column: "Nev");

            migrationBuilder.CreateIndex(
                name: "IX_Felhasznalo_Felhasznalonev",
                table: "Felhasznalo",
                column: "Felhasznalonev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FelhasznaloLeltarkorzet_JogosultKorzetekId",
                table: "FelhasznaloLeltarkorzet",
                column: "JogosultKorzetekId");

            migrationBuilder.CreateIndex(
                name: "IX_FelhasznaloSzerepkor_SzerepkorokId",
                table: "FelhasznaloSzerepkor",
                column: "SzerepkorokId");

            migrationBuilder.CreateIndex(
                name: "IX_Helyiseg_Epulet_Szobaszam",
                table: "Helyiseg",
                columns: new[] { "Epulet", "Szobaszam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Helyiseg_Vonalkod",
                table: "Helyiseg",
                column: "Vonalkod",
                unique: true,
                filter: "[Vonalkod] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportFutas_FelhasznaloId",
                table: "ImportFutas",
                column: "FelhasznaloId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportFutas_Idopont",
                table: "ImportFutas",
                column: "Idopont");

            migrationBuilder.CreateIndex(
                name: "IX_ImportHiba_ImportFutasId",
                table: "ImportHiba",
                column: "ImportFutasId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiegeszito_FoEszkozId",
                table: "Kiegeszito",
                column: "FoEszkozId");

            migrationBuilder.CreateIndex(
                name: "IX_Kiegeszito_KiegeszitoEszkozId",
                table: "Kiegeszito",
                column: "KiegeszitoEszkozId");

            migrationBuilder.CreateIndex(
                name: "IX_KiegeszitoEllenorzes_KiegeszitoId",
                table: "KiegeszitoEllenorzes",
                column: "KiegeszitoId");

            migrationBuilder.CreateIndex(
                name: "IX_KiegeszitoEllenorzes_LeolvasasId_KiegeszitoId",
                table: "KiegeszitoEllenorzes",
                columns: new[] { "LeolvasasId", "KiegeszitoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KodTipus_Nev",
                table: "KodTipus",
                column: "Nev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeltarIdoszak_LezartaId",
                table: "LeltarIdoszak",
                column: "LezartaId");

            migrationBuilder.CreateIndex(
                name: "IX_Leltarkorzet_Kod",
                table: "Leltarkorzet",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_AktivKorzetId",
                table: "Leolvasas",
                column: "AktivKorzetId");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_BeolvasottKod",
                table: "Leolvasas",
                column: "BeolvasottKod");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_EszkozId",
                table: "Leolvasas",
                column: "EszkozId");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_EszkozKodId",
                table: "Leolvasas",
                column: "EszkozKodId");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_FelhasznaloId",
                table: "Leolvasas",
                column: "FelhasznaloId");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_HelyisegId",
                table: "Leolvasas",
                column: "HelyisegId");

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_LeltarIdoszakId_EszkozId",
                table: "Leolvasas",
                columns: new[] { "LeltarIdoszakId", "EszkozId" })
                .Annotation("SqlServer:Include", new[] { "Mennyiseg", "Minosites", "Sztornozva" });

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_LeltarIdoszakId_Idopont",
                table: "Leolvasas",
                columns: new[] { "LeltarIdoszakId", "Idopont" });

            migrationBuilder.CreateIndex(
                name: "IX_Leolvasas_SztornoztaId",
                table: "Leolvasas",
                column: "SztornoztaId");

            migrationBuilder.CreateIndex(
                name: "IX_Szerepkor_Nev",
                table: "Szerepkor",
                column: "Nev",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllapotValtozas");

            migrationBuilder.DropTable(
                name: "AuditNaplo");

            migrationBuilder.DropTable(
                name: "Elhelyezes");

            migrationBuilder.DropTable(
                name: "ElvartTetel");

            migrationBuilder.DropTable(
                name: "FelelosHozzarendeles");

            migrationBuilder.DropTable(
                name: "FelhasznaloLeltarkorzet");

            migrationBuilder.DropTable(
                name: "FelhasznaloSzerepkor");

            migrationBuilder.DropTable(
                name: "ImportHiba");

            migrationBuilder.DropTable(
                name: "KiegeszitoEllenorzes");

            migrationBuilder.DropTable(
                name: "FelelosSzemely");

            migrationBuilder.DropTable(
                name: "Szerepkor");

            migrationBuilder.DropTable(
                name: "ImportFutas");

            migrationBuilder.DropTable(
                name: "Kiegeszito");

            migrationBuilder.DropTable(
                name: "Leolvasas");

            migrationBuilder.DropTable(
                name: "EszkozKod");

            migrationBuilder.DropTable(
                name: "Helyiseg");

            migrationBuilder.DropTable(
                name: "LeltarIdoszak");

            migrationBuilder.DropTable(
                name: "Eszkoz");

            migrationBuilder.DropTable(
                name: "KodTipus");

            migrationBuilder.DropTable(
                name: "Felhasznalo");

            migrationBuilder.DropTable(
                name: "EszkozAllapot");

            migrationBuilder.DropTable(
                name: "EszkozTipus");

            migrationBuilder.DropTable(
                name: "Leltarkorzet");
        }
    }
}
