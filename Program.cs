using RoadInfrastructureAssetManagementFrontend.Interface;
using RoadInfrastructureAssetManagementFrontend.Service;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7030");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Đăng ký AssetsService sử dụng IHttpClientFactory
builder.Services.AddScoped<IAssetCagetoriesService, AssetCagetoriesService>();
builder.Services.AddScoped<IAssetsService, AssetsService>();
builder.Services.AddScoped<IBudgetsService, BudgetsService>();
builder.Services.AddScoped<ICostsService, CostsService>();
builder.Services.AddScoped<IIncidentHistoriesService, IncidentHistoriesService>();
builder.Services.AddScoped<IIncidentsService, IncidentsService>();
builder.Services.AddScoped<ITasksService, TasksService>();
builder.Services.AddScoped<IUsersService, UsersService>();



var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
