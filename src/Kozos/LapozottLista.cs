namespace LeltarKezelo.Kozos;

public sealed record LapozottLista<T>(IReadOnlyList<T> Elemek, int Oldal, int OldalMeret, int Osszesen)
{
    public int OldalakSzama => OldalMeret == 0 ? 0 : (int)Math.Ceiling(Osszesen / (double)OldalMeret);
}
