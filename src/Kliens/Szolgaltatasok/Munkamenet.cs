using CommunityToolkit.Mvvm.ComponentModel;
using LeltarKezelo.Kozos.Auth;

namespace Leltarkezelo.Client.Szolgaltatasok;

/// <summary>
/// A bejelentkezett felhasználó és a token. A token csak a memóriában él (D-015): a program bezárásával a
/// munkamenet véget ér, és lejárat után újra be kell jelentkezni.
/// </summary>
public sealed partial class Munkamenet(ApiKliens api) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Bejelentkezve), nameof(Leltarozhat), nameof(Importalhat), nameof(SzerepkorNev))]
    private FelhasznaloAdatok? _felhasznalo;

    public DateTime? Lejarat { get; private set; }

    public bool Bejelentkezve => Felhasznalo is not null;

    /// <summary>Beolvashat a leltározó képernyőn (jogosultsági mátrix: Admin, Leltárfelelős, Leltározó).</summary>
    public bool Leltarozhat => Szerepkorben(Szerepkorok.Admin, Szerepkorok.Leltarfelelos, Szerepkorok.Leltarozo);

    /// <summary>Importálhat Excelből (jogosultsági mátrix: Admin, Leltárfelelős).</summary>
    public bool Importalhat => Szerepkorben(Szerepkorok.Admin, Szerepkorok.Leltarfelelos);

    public string SzerepkorNev => Felhasznalo?.Szerepkorok.FirstOrDefault() switch
    {
        Szerepkorok.Admin => "Adminisztrátor",
        Szerepkorok.Leltarfelelos => "Leltárfelelős",
        Szerepkorok.Leltarozo => "Leltározó",
        Szerepkorok.Megtekinto => "Megtekintő",
        null => "",
        var egyeb => egyeb,
    };

    public void Belepes(BejelentkezesValasz valasz)
    {
        api.TokenBeallitasa(valasz.Token);
        Lejarat = valasz.Lejarat;
        Felhasznalo = valasz.Felhasznalo;
    }

    public void Kilepes()
    {
        api.TokenBeallitasa(null);
        Lejarat = null;
        Felhasznalo = null;
    }

    private bool Szerepkorben(params string[] szerepkorok) =>
        Felhasznalo?.Szerepkorok.Any(szerepkorok.Contains) == true;
}
