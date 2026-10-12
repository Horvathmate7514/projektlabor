using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Leltarkezelo.Client.ViewModels;

namespace Leltarkezelo.Client.Views;

public partial class BejelentkezesNezet : UserControl
{
    public BejelentkezesNezet()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is INotifyPropertyChanged regi)
                regi.PropertyChanged -= ModellValtozott;
            if (e.NewValue is INotifyPropertyChanged uj)
                uj.PropertyChanged += ModellValtozott;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                Dispatcher.BeginInvoke(() => Keyboard.Focus(FelhasznalonevMezo.Text.Length == 0 ? FelhasznalonevMezo : JelszoMezo));
        };
    }

    // A PasswordBox jelszava biztonsági okból nem köthető adatkötéssel, ezért itt kerül át a nézetmodellbe.
    private void JelszoValtozott(object sender, RoutedEventArgs e)
    {
        if (DataContext is BejelentkezesViewModel vm)
            vm.Jelszo = JelszoMezo.Password;
    }

    private void ModellValtozott(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BejelentkezesViewModel.Jelszo) && sender is BejelentkezesViewModel { Jelszo: "" })
            JelszoMezo.Clear();
    }
}
