using Cbc.News.Worker;
using Cbc.News.Worker.Messaging;
using Cbc.News.Worker.Options;
using Cbc.News.Worker.Services;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("Mongo"));

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(builder.Configuration["Mongo:Database"]);
});

builder.Services.AddHttpClient<CbcRssIngestionService>();

builder.Services.AddSingleton<IStoryRepository, StoryRepository>();
builder.Services.AddSingleton<IngestionJobRepository>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IEventPublisher, ConsoleEventPublisher>();
builder.Services.AddSingleton<EventRepository>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var storyRepo = scope.ServiceProvider.GetRequiredService<IStoryRepository>();
    await storyRepo.EnsureIndexesAsync();

    var jobRepo = scope.ServiceProvider.GetRequiredService<IngestionJobRepository>();
    await jobRepo.EnsureIndexesAsync();
}

await host.RunAsync();