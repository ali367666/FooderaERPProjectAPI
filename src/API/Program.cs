using API.Extensions;
using Application.Common.Mappings.Marker;
using FluentValidation;
using Infrastructure.DependencyInjection;
using Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddJwtSwagger();

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddAutoMapper(cfg =>
{
}, typeof(ApplicationAssemblyReference).Assembly);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyReference).Assembly);


builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();

var app = builder.Build();

await app.SeedIdentityAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

