using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Ocelot Config
// builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Deployment
builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);



// 2. Add Ocelot Services
builder.Services.AddOcelot(builder.Configuration);

// ✅ 3. Add CORS (YENİ)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ✅ 4. Use CORS BEFORE Ocelot (YENİ - ÖNEMLİ: Ocelot'tan ÖNCE olmalı)
app.UseCors("AllowFrontend");

// 5. Use Ocelot Middleware
await app.UseOcelot();

app.Run();