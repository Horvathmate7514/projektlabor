using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Leltarkezelo.Client.Szolgaltatasok;
using Leltarkezelo.Client.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Leltarkezelo.Client;

public partial class App : Application
{
    private ServiceProvider? _szolgaltatasok;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += VaratlanHiba;

        var beallitasok = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();
        var szerverCim = beallitasok["Szerver:Cim"] ?? "http://localhost:5000/";

        _szolgaltatasok = Szolgaltatasok(new Uri(szerverCim.EndsWith('/') ? szerverCim : szerverCim + "/"));

        var ablak = new MainWindow { DataContext = _szolgaltatasok.GetRequiredService<FoAblakViewModel>() };
        MainWindow = ablak;
        ablak.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _szolgaltatasok?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider Szolgaltatasok(Uri szerverCim)
    {
        var s = new ServiceCollection();
        s.AddSingleton(new HttpClient { BaseAddress = szerverCim, Timeout = TimeSpan.FromSeconds(60) });
        s.AddSingleton<ApiKliens>();
        s.AddSingleton<Munkamenet>();
        s.AddSingleton<Parbeszedek>();
        s.AddSingleton<BejelentkezesViewModel>();
        s.AddSingleton<FoAblakViewModel>();
        s.AddTransient<LeltarozasViewModel>();
        s.AddTransient<EszkozokViewModel>();
        s.AddTransient<ImportViewModel>();
        s.AddTransient<Func<LeltarozasViewModel>>(sp => sp.GetRequiredService<LeltarozasViewModel>);
        s.AddTransient<Func<EszkozokViewModel>>(sp => sp.GetRequiredService<EszkozokViewModel>);
        s.AddTransient<Func<ImportViewModel>>(sp => sp.GetRequiredService<ImportViewModel>);
        return s.BuildServiceProvider();
    }

    private void VaratlanHiba(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var naplo = Path.Combine(AppContext.BaseDirectory, "hibanaplo.txt");
        try
        {
            File.AppendAllText(naplo, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }

        MessageBox.Show($"Váratlan hiba történt: {e.Exception.Message}\n\nA részletek: {naplo}",
            "Leltárkezelő", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
