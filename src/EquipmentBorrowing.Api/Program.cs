using System.Globalization;
using EquipmentBorrowing.Api;
using EquipmentBorrowing.Api.Endpoints;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Persistence;
using EquipmentBorrowing.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' is not configured. See the README.");

IClock clock = new SystemClock();
var fixedDateText = builder.Configuration["EQUIPMENT_BORROWING_FIXED_DATE"];
if (!string.IsNullOrWhiteSpace(fixedDateText))
{
    if (!DateOnly.TryParseExact(fixedDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fixedDate))
        throw new InvalidOperationException("EQUIPMENT_BORROWING_FIXED_DATE must use the format yyyy-MM-dd.");

    clock = new FixedClock(fixedDate);
}

builder.Services.AddDbContextFactory<EquipmentBorrowingDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IStudentRepository, EfStudentRepository>();
builder.Services.AddSingleton<IEquipmentRepository, EfEquipmentRepository>();
builder.Services.AddSingleton<IBorrowingRepository, EfBorrowingRepository>();
builder.Services.AddSingleton<IBorrowingTransactions, EfBorrowingTransactions>();
builder.Services.AddSingleton<IBorrowingQueries, EfBorrowingQueries>();
builder.Services.AddSingleton<IFailureInjector, EnvironmentFailureInjector>();
builder.Services.AddSingleton(clock);

builder.Services.AddTransient<BorrowEquipmentService>();
builder.Services.AddTransient<ReturnEquipmentService>();
builder.Services.AddTransient<MultiCheckoutService>();
builder.Services.AddTransient<StudentManagementService>();
builder.Services.AddTransient<EquipmentManagementService>();
builder.Services.AddTransient<BorrowingHistoryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Campus Equipment Borrowing API", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler(errors => errors.Run(ApiErrors.WriteServerErrorAsync));
app.UseStatusCodePages(context => ApiErrors.WriteStatusCodeAsync(context.HttpContext));

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Campus Equipment Borrowing API v1"));

if (clock is FixedClock fixedClock)
    app.Logger.LogWarning("The application date is fixed at {Date} for testing.", fixedClock.Today);

if (!string.Equals(app.Configuration["EQUIPMENT_BORROWING_SKIP_MIGRATION"], "1", StringComparison.Ordinal))
{
    try
    {
        var factory = app.Services.GetRequiredService<IDbContextFactory<EquipmentBorrowingDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(
            "Could not apply migrations at startup ({ErrorType}). The API will keep running and database requests will return 500 until the database is reachable.",
            ex.GetType().Name);
    }
}

app.MapReadEndpoints();
app.MapWriteEndpoints();

app.Run();
