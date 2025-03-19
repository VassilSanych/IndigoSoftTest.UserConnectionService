using System;
using System.Net;
using System.Runtime.InteropServices;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserConnectionService.Data;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
	private readonly AppDbContext _context;

	public ConnectionController(AppDbContext context)
	{
		_context = context;
	}

	[HttpPost]
	public async Task<IActionResult> LogConnection(long userId, string ipAddress)
	{
		if (!IPAddress.TryParse(ipAddress, out _))
			return BadRequest("Invalid IP address");

		var connection = new UserConnection
		{
			UserId = userId,
			IpAddress = ipAddress,
			Timestamp = DateTime.UtcNow
		};

		_context.UserConnections.Add(connection);
		await _context.SaveChangesAsync();

		return Ok();
	}

	[HttpGet("users/by-ip/{partialIp}")]
	public async Task<IActionResult> FindUsersByIp(string partialIp)
	{
		var users = await _context.UserConnections
			.Where(uc => EF.Functions.Like(uc.IpAddress, $"{partialIp}%"))
			.Select(uc => uc.UserId)
			.Distinct()
			.ToListAsync();

		return Ok(users);
	}

	[HttpGet("users/{userId}/ips")]
	public async Task<IActionResult> GetUserIps(long userId)
	{
		var ips = await _context.UserConnections
			.Where(uc => uc.UserId == userId)
			.Select(uc => uc.IpAddress)
			.Distinct()
			.ToListAsync();

		return Ok(ips);
	}

	[HttpGet("users/{userId}/last-connection")]
	public async Task<IActionResult> GetLastConnection(long userId)
	{
		var lastConnection = await _context.UserConnections
			.Where(uc => uc.UserId == userId)
			.OrderByDescending(uc => uc.Timestamp)
			.Select(uc => new { uc.IpAddress, uc.Timestamp })
			.FirstOrDefaultAsync();

		return Ok(lastConnection);
	}
}
