using CommunityToolkit.Mvvm.ComponentModel;
using Leltarkezelo.Client.Szolgaltatasok;

namespace Leltarkezelo.Client.ViewModels;

/// <summary>A menüből elérhető oldalak közös alapja: betöltés, foglaltság és hibaüzenet kezelése.</summary>
public abstract partial class OldalViewModel : ObservableObject
{
    private bool _betoltve;

    [ObservableProperty]
    private bool _foglalt;

    [ObservableProperty]
    private string? _hiba;

    public abstract string Cim { get; }

    /// <summary>Az oldal első megjelenítésekor egyszer fut le; a menüben visszalépve az állapot megmarad.</summary>
    public async Task MegjelenitesAsync()
    {
        if (_betoltve)
            return;
        _betoltve = true;
        await BetoltesAsync();
    }

    protected virtual Task BetoltesAsync() => Task.CompletedTask;

    /// <summary>Szerverhívás futtatása: közben foglalt, a hibát az oldal tetején jeleníti meg.</summary>
    protected async Task<bool> FuttatasAsync(Func<Task> muvelet)
    {
        Foglalt = true;
        Hiba = null;
        try
        {
            await muvelet();
            return true;
        }
        catch (ApiHiba ex)
        {
            Hiba = ex.Message;
            return false;
        }
        finally
        {
            Foglalt = false;
        }
    }
}
