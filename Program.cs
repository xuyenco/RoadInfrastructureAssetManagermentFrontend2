using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Service;
using Serilog;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

//Serilog setting for logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            "logs/frontend.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Add services to the container.
builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    options.Filters.Add<LoginFilter>(); // Áp dụng filter toàn cục
});

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7030");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Đăng ký IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Đăng ký các service
builder.Services.AddScoped<IAssetCagetoriesService, AssetCagetoriesService>();
builder.Services.AddScoped<IAssetsService, AssetsService>();
builder.Services.AddScoped<IBudgetsService, BudgetsService>();
builder.Services.AddScoped<ICostsService, CostsService>();
builder.Services.AddScoped<IIncidentHistoriesService, IncidentHistoriesService>();
builder.Services.AddScoped<IIncidentsService, IncidentsService>();
builder.Services.AddScoped<IIncidentImageService, IncidentImageService>();
builder.Services.AddScoped<ITasksService, TasksService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IMaintenanceHistoryService,MaintenanceHistoryService>();
builder.Services.AddScoped<IMaintenanceDocumentService,MaintenanceDocumentService>();
// Setting Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use session
app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();