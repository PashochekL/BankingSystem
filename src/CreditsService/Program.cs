using System.Text;
using CreditsService.Data;
using CreditsService.Jobs;
using CreditsService.Middleware;
using CreditsService.Options;
using CreditsService.Repositories;
using CreditsService.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var creditsDbConnectionString = builder.Configuration.GetConnectionString("CreditsDb")
    ?? throw new InvalidOperationException("Connection string 'CreditsDb' is not configured.");

var hangfireDbConnectionString = builder.Configuration.GetConnectionString("HangfireDb")
    ?? throw new InvalidOperationException("Connection string 'HangfireDb' is not configured.");

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT options are not configured.");

if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("JWT secret is not configured.");
}

builder.Services.AddDbContext<CreditsDbContext>(options =>
    options.UseNpgsql(creditsDbConnectionString));

builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<ICreditTariffRepository, CreditTariffRepository>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ICreditTariffService, CreditTariffService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IInterestAccrualJob, InterestAccrualJob>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHangfire(configuration =>
    configuration.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(hangfireDbConnectionString)));

builder.Services.AddHangfireServer();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

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
