using System.Collections.ObjectModel;
using System.Windows.Input;
using Leltarkezelo.Client.Infrastructure;

namespace Leltarkezelo.Client.ViewModels;

// Kizárólag a látványterv állapotát tartja. Az üzleti adatokat később az API adja.
public sealed class MainViewModel : ObservableObject
{
    private string _currentPage = "Áttekintés";
    private string _scanCode = string.Empty;
    private string _feedback = "Készen áll a beolvasásra";
    private string _searchText = string.Empty;

    public ObservableCollection<object> Equipment { get; } = new();
    public string CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); } }
    public string ScanCode { get => _scanCode; set { _scanCode = value; OnPropertyChanged(); } }
    public string Feedback { get => _feedback; set { _feedback = value; OnPropertyChanged(); } }
    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
    public IEnumerable<object> FilteredEquipment => Equipment;
    public int FoundCount { get => 0; set { } }
    public int TotalCount { get => 0; set { } }
    public int Progress { get => 0; set { } }
    public ICommand ScanCommand { get; }

    public MainViewModel() => ScanCommand = new RelayCommand(Scan);
    public void Navigate(string page) => CurrentPage = page;
    private void Scan()
    {
        Feedback = "A leolvasás eredményét később a szerver adja vissza.";
        ScanCode = string.Empty;
    }
}
