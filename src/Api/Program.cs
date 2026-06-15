using EventHub.Api;
using EventHub.Api.Endpoints;
using EventHub.Api.Hubs;
using EventHub.Application;
using EventHub.Infrastructure;
using EventHub.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Environment);

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.MapDefaultEndpoints();
app.UseApiPipeline(app.Environment);

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapEndpoints(EventHub.Api.AssemblyReference.Assembly);
app.MapHub<EventMonitoringHub>("/hubs/events");

app.Run();

public partial class Program;
