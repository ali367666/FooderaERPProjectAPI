using API.Extensions;
using API.Middlewares;
using Application.Common.Behaviors;
using Application.Common.Mappings.Marker;
using FluentValidation;
using Infrastructure.DependencyInjection;
using Infrastructure.Extensions;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

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
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();

var app = builder.Build();

await app.SeedIdentityAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();