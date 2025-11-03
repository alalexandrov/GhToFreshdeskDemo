using GhToFreshdesk.Application;
using GhToFreshdesk.Infrastructure;
using GhToFreshdesk.Infrastructure.Persistence;
using GhToFreshdesk.WebApi.Middleware;
using GhToFreshdesk.WebApi.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<SyncJobWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseGlobalExceptionHandling();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();