namespace Leltarkezelo.Client.ViewModels;

/// <summary>A tervben szereplő, de még el nem készült oldal: megmutatja, mi kerül ide és mikor.</summary>
public sealed class KesobbViewModel(string cim, string alkalom, string leiras, IReadOnlyList<string> tartalom) : OldalViewModel
{
    public override string Cim { get; } = cim;
    public string Alkalom { get; } = alkalom;
    public string Leiras { get; } = leiras;
    public IReadOnlyList<string> Tartalom { get; } = tartalom;
}
