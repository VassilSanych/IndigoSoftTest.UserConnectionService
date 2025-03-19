using System.ComponentModel;
using System.Net;
using Microsoft.EntityFrameworkCore;
// Add this using directive

using UserConnectionService;

var builder = WebApplication.CreateBuilder(args);

// Добавление конфигурации из переменных окружения
builder.Configuration.AddEnvironmentVariables();

// Регистрация сервисов
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(builder.Configuration["DB_CONNECTION"]));

var app = builder.Build();

// Middleware для обработки запросов
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Применение миграций при старте
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	dbContext.Database.Migrate();
}


app.MapPost("/api/connection", async (
	[Description("Контекст базы данных.")] AppDbContext db,
	[Description("ID пользователя.")] long userId, 
	[Description("IP-адрес пользователя.")] string ipAddress) =>
{
	if (!IPAddress.TryParse(ipAddress, out _))
		return Results.BadRequest("Invalid IP address");

	var connection = new UserConnection
	{
		UserId = userId,
		IpAddress = ipAddress,
		Timestamp = DateTime.UtcNow
	};

	await db.UserConnections.AddAsync(connection);
	await db.SaveChangesAsync();

	return Results.Ok();
})
.WithName("LogConnection")
.WithOpenApi(operation => new(operation)
{
	Summary = "Логируем событие подключения пользователя.",
	Description = "HTTP 200 OK при успешном добавлении."
});


app.MapGet("/api/users/by-ip/{partialIp}", async (
	[Description("Контекст базы данных.")] AppDbContext db, 
	[Description("Частичная строка IP-адреса.")] string partialIp) =>
{
	var users = await db.UserConnections
		.Where(uc => EF.Functions.Like(uc.IpAddress, $"{partialIp}%"))
		.Select(uc => uc.UserId)
		.Distinct()
		.ToListAsync();

	return Results.Ok(users);
})
.WithName("FindUsersByIp")
.WithOpenApi(operation => new(operation)
{
	Summary = "Находим всех пользователей, чьи IP-адреса начинаются с указанной подстроки.",
	Description = "Список ID пользователей."
});


app.MapGet("/api/users/{userId}/ips", async (
	[Description("Контекст базы данных.")]AppDbContext db,
	[Description("ID пользователя.")] long userId) =>
{
	var ips = await db.UserConnections
		.Where(uc => uc.UserId == userId)
		.Select(uc => uc.IpAddress)
		.Distinct()
		.ToListAsync();

	return Results.Ok(ips);
})
.WithName("GetUserIps")
.WithOpenApi(operation => new(operation)
{
	Summary = "Получаем все уникальные IP-адреса, связанные с указанным пользователем.",
	Description = "Список уникальных IP-адресов."
});


app.MapGet("/api/users/{userId}/last-connection", async (
	[Description("Контекст базы данных.")]AppDbContext db, 
	[Description("ID пользователя.")]long userId) =>

{
	var lastConnection = await db.UserConnections
		.Where(uc => uc.UserId == userId)
		.OrderByDescending(uc => uc.Timestamp)
		.Select(uc => new { uc.IpAddress, uc.Timestamp })
		.FirstOrDefaultAsync();

	return Results.Ok(lastConnection);
})
.WithName("GetLastConnection")
.WithOpenApi(operation => new(operation)
{
	Summary = "Возвращаем данные о последнем подключении пользователя.",
	Description = "Объект с полями IpAddress и Timestamp, либо null, если записей нет."
});


// Регистрация OpenAPI
app.MapOpenApi();


app.Run();