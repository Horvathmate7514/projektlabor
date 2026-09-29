using System.Windows;
using Leltarkezelo.Client.ViewModels;

namespace Leltarkezelo.Client;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    private void Show(string page, UIElement panel)
    {
        ((MainViewModel)DataContext).Navigate(page);
        OverviewPanel.Visibility = Visibility.Collapsed; InventoryPanel.Visibility = Visibility.Collapsed; EquipmentPanel.Visibility = Visibility.Collapsed;
        ComparisonPanel.Visibility = Visibility.Collapsed; ReportsPanel.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;
    }
    private void Overview_Click(object sender, RoutedEventArgs e) => Show("Áttekintés", OverviewPanel);
    private void Inventory_Click(object sender, RoutedEventArgs e) => Show("Leltározás", InventoryPanel);
    private void Equipment_Click(object sender, RoutedEventArgs e) => Show("Eszközök", EquipmentPanel);
    private void Comparison_Click(object sender, RoutedEventArgs e) => Show("Összehasonlítás", ComparisonPanel);
    private void Reports_Click(object sender, RoutedEventArgs e) => Show("Import és riportok", ReportsPanel);
}
