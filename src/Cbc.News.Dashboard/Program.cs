using Cbc.News.Dashboard.Hubs;
using Cbc.News.Dashboard.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IngestionNotifier>();
builder.Services.AddSingleton<EventService>();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration["Mongo:ConnectionString"];
    return new MongoClient(connectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["Mongo:Database"];
    return client.GetDatabase(databaseName);
});

builder.Services.AddHttpClient("api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddHttpClient("api", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
});

// builder.Services.AddHostedService<IngestionStartedConsumer>();
// builder.Services.AddHostedService<IngestionCompletedConsumer>();
// builder.Services.AddHostedService<IngestionFailedConsumer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapHub<IngestionHub>("/hubs/ingestion");

app.Run();