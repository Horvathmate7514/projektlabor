using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LeltarKezelo.Szerver.Adat.Konfiguraciok;

/// <summary>
/// A technikai felsorolások az adatbázisban nagybetűs, aláhúzással tagolt szövegként tárolódnak
/// (pl. <c>MasKorzet</c> → <c>MAS_KORZET</c>), CHECK megszorítással védve (adatmodell v1, 5. pont).
/// </summary>
public static class Felsorolas
{
    public const int Hossz = 20;

    public static string AdatbazisErtek<T>(T ertek) where T : struct, Enum
    {
        var nev = ertek.ToString();
        var sb = new StringBuilder(nev.Length + 4);
        for (var i = 0; i < nev.Length; i++)
        {
            if (i > 0 && char.IsUpper(nev[i])) sb.Append('_');
            sb.Append(char.ToUpperInvariant(nev[i]));
        }
        return sb.ToString();
    }

    public static T Beolvas<T>(string ertek) where T : struct, Enum =>
        Enum.GetValues<T>().Single(e => AdatbazisErtek(e) == ertek);

    public static PropertyBuilder<T> SzovegkentTarolva<T>(this PropertyBuilder<T> property) where T : struct, Enum =>
        property
            .HasConversion(new ValueConverter<T, string>(e => AdatbazisErtek(e), s => Beolvas<T>(s)))
            .HasMaxLength(Hossz)
            .IsUnicode(false);

    public static PropertyBuilder<T?> SzovegkentTarolva<T>(this PropertyBuilder<T?> property) where T : struct, Enum =>
        property
            .HasConversion(new ValueConverter<T?, string?>(
                e => e.HasValue ? AdatbazisErtek(e.Value) : null,
                s => s == null ? null : Beolvas<T>(s)))
            .HasMaxLength(Hossz)
            .IsUnicode(false);

    /// <summary>CHECK feltétel: az oszlop csak a felsorolás értékeit veheti fel.</summary>
    public static string Ellenorzes<T>(string oszlop) where T : struct, Enum =>
        $"[{oszlop}] IN ({string.Join(", ", Enum.GetValues<T>().Select(e => $"'{AdatbazisErtek(e)}'"))})";
}
