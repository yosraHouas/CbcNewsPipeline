using System.Text;
using Cbc.News.Api.Hubs;
using Cbc.News.Api.Messaging;
using Cbc.News.Api.Middleware;
using Cbc.News.Api.Options;
using Cbc.News.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using System.Security.Claims;




var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cbc.News.Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddSignalR();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT auth failed:");
                Console.WriteLine(context.Exception.ToString());
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("JWT challenge triggered.");
                Console.WriteLine($"Error: {context.Error}");
                Console.WriteLine($"Description: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(builder.Configuration["Mongo:Database"]);
});

builder.Services.AddSingleton<LogRepository>();
builder.Services.AddSingleton<StoryRepository>();
builder.Services.AddSingleton<IngestionJobRepository>();
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<NotificationRepository>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddHostedService<IngestionEventsConsumer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardCors", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<LogRepository>();
    await repo.EnsureIndexesAsync();

    var storyRepo = scope.ServiceProvider.GetRequiredService<StoryRepository>();
    await storyRepo.EnsureIndexesAsync();

    var jobRepo = scope.ServiceProvider.GetRequiredService<IngestionJobRepository>();
    await jobRepo.EnsureIndexesAsync();

    var notificationRepository = scope.ServiceProvider.GetRequiredService<NotificationRepository>();
    await notificationRepository.EnsureIndexesAsync();

    var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
    await userRepository.EnsureIndexesAsync();
    await userRepository.SeedIfEmptyAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("DashboardCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();