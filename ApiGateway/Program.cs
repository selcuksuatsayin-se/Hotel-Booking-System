using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Ocelot Config
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. Add Ocelot Services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// 3. Use Ocelot Middleware
await app.UseOcelot();

app.Run();