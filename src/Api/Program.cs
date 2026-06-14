using Scalar.AspNetCore;
using Solution.Api;
using Solution.Api.Endpoints;
using Solution.Application;
using Solution.Infrastructure;
using Solution.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
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
app.MapEndpoints(Solution.Api.AssemblyReference.Assembly);

app.Run();

public partial class Program;
