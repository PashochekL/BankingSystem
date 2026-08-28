using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UsersService.Data;
using UsersService.Entities;
using UsersService.Middleware;
using UsersService.Options;
using UsersService.Repositories;
using UsersService.Services;

var builder = WebApplication.CreateBuilder(args);

var usersDbConnectionString = builder.Configuration.GetConnectionString("UsersDb")
    ?? throw new InvalidOperationException("Connection string 'UsersDb' is not configured.");

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(usersDbConnectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

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

app.MapControllers();

app.Run();
