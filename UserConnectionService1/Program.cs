using System;

using Microsoft.EntityFrameworkCore;

using UserConnectionService.Data;

var builder = WebApplication.CreateBuilder(args);

// Добавление конфигурации из переменных окружения
builder.Configuration.AddEnvironmentVariables();

// Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration["DB_CONNECTION"]));

var app = builder.Build();

// Middleware для обработки запросов
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Применение миграций при старте
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	dbContext.Database.Migrate();
}

app.Run();