using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        SeedDemoData();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
        services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
        services.AddSingleton<IBorrowingRepository, InMemoryBorrowingRepository>();

        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }

    private static void SeedDemoData()
    {
        var students = (InMemoryStudentRepository)Services.GetRequiredService<IStudentRepository>();
        var equipment = (InMemoryEquipmentRepository)Services.GetRequiredService<IEquipmentRepository>();

        students.Seed(new Student(1, "Juan Dela Cruz"));
        students.Seed(new Student(2, "Maria Santos"));
        students.Seed(new Student(3, "Pedro Reyes", isAllowedToBorrow: false));

        equipment.Seed(new Equipment(100, "Digital Multimeter", "Measuring instrument"));
        equipment.Seed(new Equipment(101, "Oscilloscope", "Waveform display", isAvailable: false));
        equipment.Seed(new Equipment(102, "Soldering Iron", "Electronics tool"));
        equipment.Seed(new Equipment(103, "Function Generator", "Signal generator"));
    }
}
