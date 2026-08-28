using Microsoft.EntityFrameworkCore;
using UsersService.Data;
using UsersService.Repositories;
using UsersService.Services;

var builder = WebApplication.CreateBuilder(args);

var usersDbConnectionString = builder.Configuration.GetConnectionString("UsersDb")
    ?? throw new InvalidOperationException("Connection string 'UsersDb' is not configured.");

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(usersDbConnectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
