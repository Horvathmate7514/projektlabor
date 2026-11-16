using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Leltarkezelo.Client.Szolgaltatasok;
using LeltarKezelo.Kozos.Leltar;
using LeltarKezelo.Kozos.Torzsadatok;

namespace Leltarkezelo.Client.ViewModels;

public sealed record NaploSor(LeolvasasListaElem Adat)
{
    public string Idopont => Minositesek.HelyiIdo(Adat.Idopont).ToString("yyyy. MM. dd. HH:mm:ss");
    public string Kod => Adat.BeolvasottKod;
    public string Eszkoz => Adat.EszkozMegnevezes ?? "Ismeretlen eszköz";
    public string Korzet => Adat.AktivKorzetKod;
    public int Mennyiseg => Adat.Mennyiseg;
    public string Felhasznalo => Adat.Felhasznalo;
    public string BeviteliMod => Minositesek.Nev(Adat.BeviteliMod);
    public string Minosites => Minositesek.Nev(Adat.Minosites);
    public string Statusz => Adat.Sztornozva ? "Sztornózott" : "Érvényes";
}

/// <summary>Nyitott és lezárt időszakok leolvasásainak lapozott, szerveroldalon szűrt nézete.</summary>
public sealed partial class LeolvasasiNaploViewModel(ApiKliens api, Munkamenet munkamenet) : OldalViewModel
{
    private bool _szurokBetoltese;
    private int _listaVerzio;
    public override string Cim => "Leolvasási napló";
    public bool Engedelyezett => munkamenet.Leltarozhat;
    public ObservableCollection<IdoszakSor> Idoszakok { get; } = [];
    public ObservableCollection<Valasztas> Korzetek { get; } = [];
    public ObservableCollection<NaploSor> Sorok { get; } = [];
    public IReadOnlyList<int> OldalMeretek { get; } = [25, 50, 100, 200];

    [ObservableProperty] private IdoszakSor? _idoszak;
    [ObservableProperty] private Valasztas? _korzet;
    [ObservableProperty] private bool _csakSajat = true;
    [ObservableProperty] private bool _sztornozottakkalEgyutt;
    [ObservableProperty] private int _oldalMeret = 50;
    [ObservableProperty] private int _oldal = 1;
    [ObservableProperty] private int _osszesen;
    [ObservableProperty] private int _oldalakSzama;

    public bool VanElozo => Oldal > 1;
    public bool VanKovetkezo => Oldal < OldalakSzama;
    public string LapozasSzoveg => Osszesen == 0 ? "Nincs a szűrésnek megfelelő leolvasás."
        : $"{(Oldal - 1) * OldalMeret + 1}–{Math.Min(Oldal * OldalMeret, Osszesen)} / {Osszesen} leolvasás · {Oldal}/{OldalakSzama}. oldal";

    public override async Task MegjelenitesAsync()
    {
        if (!Engedelyezett) return;
        var idoszakId = Idoszak?.Id;
        var korzetId = Korzet?.Id;
        _szurokBetoltese = true;
        try
        {
            if (!await FuttatasAsync(async () =>
            {
                var idoszakok = api.LeltarIdoszakokAsync(null);
                var korzetek = api.LeltarkorzetekAsync();
                await Task.WhenAll(idoszakok, korzetek);
                Idoszakok.Clear();
                foreach (var i in await idoszakok) Idoszakok.Add(new(i));
                Korzetek.Clear();
                Korzetek.Add(new(null, "Minden jogosult körzet"));
                foreach (var k in (await korzetek).Where(k => k.Leltarozhato))
                    Korzetek.Add(new(k.Id, $"{k.Kod} – {k.Nev}"));
                Idoszak = Idoszakok.FirstOrDefault(i => i.Id == idoszakId)
                    ?? Idoszakok.FirstOrDefault(i => i.Adat.Statusz == LeltarIdoszakStatusz.Nyitott)
                    ?? Idoszakok.FirstOrDefault();
                Korzet = Korzetek.FirstOrDefault(k => k.Id == korzetId) ?? Korzetek[0];
            })) return;
        }
        finally { _szurokBetoltese = false; }
        await ListaBetolteseAsync();
    }

    partial void OnIdoszakChanged(IdoszakSor? value) => SzuroValtozott();
    partial void OnKorzetChanged(Valasztas? value) => SzuroValtozott();
    partial void OnCsakSajatChanged(bool value) => SzuroValtozott();
    partial void OnSztornozottakkalEgyuttChanged(bool value) => SzuroValtozott();
    partial void OnOldalMeretChanged(int value) => SzuroValtozott();

    private void SzuroValtozott()
    {
        if (_szurokBetoltese) return;
        Oldal = 1;
        Sorok.Clear();
        Osszesen = OldalakSzama = 0;
        LapozasFrissitese();
        _ = ListaBetolteseAsync();
    }

    [RelayCommand] private Task FrissitesAsync() => MegjelenitesAsync();
    [RelayCommand]
    private async Task OldalraAsync(string irany)
    {
        if (Foglalt) return;
        Oldal = irany switch
        {
            "elso" => 1,
            "elozo" => Math.Max(1, Oldal - 1),
            "utolso" => Math.Max(1, OldalakSzama),
            _ => Math.Min(Math.Max(1, OldalakSzama), Oldal + 1),
        };
        await ListaBetolteseAsync();
    }

    private async Task ListaBetolteseAsync()
    {
        var verzio = ++_listaVerzio;
        if (Idoszak is not { } idoszak)
        {
            Sorok.Clear();
            Osszesen = OldalakSzama = 0;
            LapozasFrissitese();
            return;
        }
        var szuro = new LeolvasasSzuro
        {
            LeltarkorzetId = Korzet?.Id, CsakSajat = CsakSajat,
            SztornozottakkalEgyutt = SztornozottakkalEgyutt, Oldal = Oldal, OldalMeret = OldalMeret,
        };
        await FuttatasAsync(async () =>
        {
            var lista = await api.LeolvasasokAsync(idoszak.Id, szuro);
            if (verzio != _listaVerzio) return;
            // Más felhasználó sztornója miatt az utolsó oldal közben kiürülhet.
            if (lista.OldalakSzama > 0 && lista.Oldal > lista.OldalakSzama)
            {
                lista = await api.LeolvasasokAsync(idoszak.Id, szuro with { Oldal = lista.OldalakSzama });
                if (verzio != _listaVerzio) return;
            }
            Sorok.Clear();
            foreach (var adat in lista.Elemek) Sorok.Add(new(adat));
            Osszesen = lista.Osszesen;
            OldalakSzama = lista.OldalakSzama;
            Oldal = lista.Oldal;
            LapozasFrissitese();
        });
    }

    private void LapozasFrissitese()
    {
        OnPropertyChanged(nameof(VanElozo));
        OnPropertyChanged(nameof(VanKovetkezo));
        OnPropertyChanged(nameof(LapozasSzoveg));
    }
}
