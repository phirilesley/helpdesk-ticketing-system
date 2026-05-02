using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Realtime.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace HelpDeskSystem.Realtime.Hubs;

[Authorize]
public class HelpDeskHub : Hub
{
    private readonly ILogger<HelpDeskHub> _logger;
    private readonly IConnectionManager _connectionManager;

    public HelpDeskHub(ILogger<HelpDeskHub> logger, IConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var tenantId = Context.User.GetTenantId();

        if (!string.IsNullOrEmpty(userId) && tenantId.HasValue)
        {
            await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId, tenantId.Value);
            
            // Join tenant group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");
            
            // Join user-specific group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Join role-based groups
            var roles = Context.User.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value);
            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
            }

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);
            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Join ticket-specific conversation
    public async Task JoinTicket(int ticketId)
    {
        var userId = Context.UserIdentifier;
        var tenantId = Context.User.GetTenantId();

        if (!string.IsNullOrEmpty(userId) && tenantId.HasValue)
        {
            // Verify user has access to this ticket
            // This would involve checking if user belongs to tenant and has permission
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
            await Clients.Caller.SendAsync("JoinedTicket", ticketId);
            
            _logger.LogInformation("User {UserId} joined ticket {TicketId}", userId, ticketId);
        }
    }

    // Leave ticket conversation
    public async Task LeaveTicket(int ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
        await Clients.Caller.SendAsync("LeftTicket", ticketId);
        
        _logger.LogInformation("User {UserId} left ticket {TicketId}", Context.UserIdentifier, ticketId);
    }

    // Join kanban board
    public async Task JoinKanban()
    {
        var tenantId = Context.User.GetTenantId();
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"kanban_{tenantId.Value}");
            await Clients.Caller.SendAsync("JoinedKanban");
        }
    }

    // Leave kanban board
    public async Task LeaveKanban()
    {
        var tenantId = Context.User.GetTenantId();
        if (tenantId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kanban_{tenantId.Value}");
            await Clients.Caller.SendAsync("LeftKanban");
        }
    }

    // Typing indicator for ticket messages
    public async Task StartTyping(int ticketId)
    {
        var userId = Context.UserIdentifier;
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.GroupExcept($"ticket_{ticketId}", Context.ConnectionId)
                .SendAsync("UserTyping", new { UserId = userId, UserName = userName, TicketId = ticketId });
        }
    }

    public async Task StopTyping(int ticketId)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.GroupExcept($"ticket_{ticketId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", new { UserId = userId, TicketId = ticketId });
        }
    }

    // Get online users for tenant
    public async Task GetOnlineUsers()
    {
        var tenantId = Context.User.GetTenantId();
        if (tenantId.HasValue)
        {
            var onlineUsers = await _connectionManager.GetOnlineUsersAsync(tenantId.Value);
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }
    }
}

public interface IConnectionManager
{
    Task AddConnectionAsync(string userId, string connectionId, int tenantId);
    Task RemoveConnectionAsync(string userId, string connectionId);
    Task<IEnumerable<string>> GetConnectionsForUserAsync(string userId);
    Task<IEnumerable<OnlineUserDto>> GetOnlineUsersAsync(int tenantId);
    Task<bool> IsUserOnlineAsync(string userId);
}

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, UserConnectionInfo> _connectionInfo = new();
    private readonly ILogger<ConnectionManager> _logger;

    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _logger = logger;
    }

    public async Task AddConnectionAsync(string userId, string connectionId, int tenantId)
    {
        var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(connectionId);
        }

        _connectionInfo[connectionId] = new UserConnectionInfo
        {
            UserId = userId,
            TenantId = tenantId,
            ConnectedAt = DateTime.UtcNow
        };

        _logger.LogDebug("Added connection {ConnectionId} for user {UserId}", connectionId, userId);
        await Task.CompletedTask;
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }

        _connectionInfo.TryRemove(connectionId, out _);
        _logger.LogDebug("Removed connection {ConnectionId} for user {UserId}", connectionId, userId);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetConnectionsForUserAsync(string userId)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                return connections.ToList();
            }
        }
        return Enumerable.Empty<string>();
    }

    public async Task<IEnumerable<OnlineUserDto>> GetOnlineUsersAsync(int tenantId)
    {
        var tenantConnections = _connectionInfo
            .Where(kvp => kvp.Value.TenantId == tenantId)
            .GroupBy(kvp => kvp.Value.UserId)
            .Select(g => new OnlineUserDto
            {
                UserId = g.Key,
                ConnectionCount = g.Count(),
                ConnectedAt = g.Min(kvp => kvp.Value.ConnectedAt)
            });

        return tenantConnections;
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        return _userConnections.ContainsKey(userId);
    }
}

public class UserConnectionInfo
{
    public string UserId { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class OnlineUserDto
{
    public string UserId { get; set; } = string.Empty;
    public int ConnectionCount { get; set; }
    public DateTime ConnectedAt { get; set; }
}
