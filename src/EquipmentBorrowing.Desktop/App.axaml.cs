using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        ConfigureServices(services, ResolveConnectionString());
        Services = services.BuildServiceProvider();

        ApplyMigrationsAndSeedData();

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

    private static string ResolveConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<App>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:Default' is not configured. See the README.");
    }

    private static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<EquipmentBorrowingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<IStudentRepository, EfStudentRepository>();
        services.AddSingleton<IEquipmentRepository, EfEquipmentRepository>();
        services.AddSingleton<IBorrowingRepository, EfBorrowingRepository>();
        services.AddSingleton<IBorrowingTransactions, EfBorrowingTransactions>();

        services.AddTransient<BorrowEquipmentService>();
        services.AddTransient<ReturnEquipmentService>();

        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<BorrowingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }

    private static void ApplyMigrationsAndSeedData()
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<EquipmentBorrowingDbContext>>();

        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();

        DatabaseSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }
}