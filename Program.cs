using SlackTimezoneTracker.Model;
using SlackTimezoneTracker.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<InMemoryWorkspaceStore>();
builder.Services.AddSingleton<SlackApiService>();
builder.Services.AddHostedService<TimezoneTrackerService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();