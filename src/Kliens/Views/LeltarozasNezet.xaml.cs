using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Leltarkezelo.Client.ViewModels;

namespace Leltarkezelo.Client.Views;

public partial class LeltarozasNezet : UserControl
{
    public LeltarozasNezet()
    {
        InitializeComponent();
        DataObject.AddPastingHandler(KodMezo, (_, _) =>
        {
            if (DataContext is LeltarozasViewModel vm)
                vm.Beillesztve = true;
        });
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is LeltarozasViewModel regi)
                regi.FokuszKell -= Fokuszalas;
            if (e.NewValue is LeltarozasViewModel uj)
                uj.FokuszKell += Fokuszalas;
        };
        Loaded += (_, _) => Fokuszalas(this, EventArgs.Empty);
    }

    // Az olvasó billentyűzetként ír: a fókusznak a beolvasó mezőben kell maradnia (F-09).
    private void Fokuszalas(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            if (KodMezo.IsEnabled && IsVisible)
                Keyboard.Focus(KodMezo);
        });
}
