using API.Extensions;
using API.Middlewares;
using API.Authorization;
using Application.Common.Behaviors;
using Application.Common.Mappings.Marker;
using FluentValidation;
using Infrastructure.DependencyInjection;
using Infrastructure.Extensions;
using MediatR;
using Serilog;

if (args.Contains("--generate-license-keypair"))
{
    var (privatePem, publicPem) = Infrastructure.Services.LicenseKeyService.GenerateKeyPair();
    Console.WriteLine("# Licensing:PrivateKeyPem — central server ONLY, keep secret:");
    Console.WriteLine(privatePem);
    Console.WriteLine("# Licensing:PublicKeyPem — every installation:");
    Console.WriteLine(publicPem);
    return;
}

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
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Application.Common.Behaviors.PartialPaymentGuardBehavior<,>));

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPermissionPolicies();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var isLocalInstallation = string.Equals(builder.Configuration["Deployment:Mode"], "Local", StringComparison.OrdinalIgnoreCase);
        if (builder.Environment.IsDevelopment() || isLocalInstallation)
        {
            // Dev, and a restaurant's Local installation (reachable only inside its own network):
            // terminals open the app via the server's LAN IP, which isn't known in advance.
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() is { Length: > 0 } configured
                ? configured
                : new[] { "http://localhost:3000" };
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<DeviceAccessMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();