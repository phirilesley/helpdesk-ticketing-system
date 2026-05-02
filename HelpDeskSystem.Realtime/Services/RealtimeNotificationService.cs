using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HelpDeskSystem.Realtime.Services;

public interface IRealtimeNotificationService
{
    Task NotifyTicketUpdatedAsync(int ticketId, object ticketData, string? updatedBy = null);
    Task NotifyTicketAssignedAsync(int ticketId, object assignmentData);
    Task NotifyTicketStatusChangedAsync(int ticketId, object statusData);
    Task NotifyNewMessageAsync(int ticketId, object messageData);
    Task NotifyUserTypingAsync(int ticketId, string userId, bool isTyping);
    Task NotifyKanbanUpdatedAsync(int tenantId, object kanbanData);
    Task NotifySlaBreachAsync(int ticketId, object slaData);
    Task NotifyWorkflowExecutedAsync(int ticketId, object workflowData);
    Task SendNotificationToUserAsync(string userId, object notificationData);
    Task SendNotificationToRoleAsync(int tenantId, string role, object notificationData);
    Task SendNotificationToTenantAsync(int tenantId, object notificationData);
}

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<HelpDeskHub> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<HelpDeskHub> hubContext,
        IConnectionManager connectionManager,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task NotifyTicketUpdatedAsync(int ticketId, object ticketData, string? updatedBy = null)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("TicketUpdated", new
                {
                    TicketId = ticketId,
                    Data = ticketData,
                    UpdatedBy = updatedBy,
                    Timestamp = DateTime.UtcNow
                });

            // Also notify tenant for kanban updates
            var tenantId = GetTenantIdFromTicketData(ticketData);
            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"kanban_{tenantId.Value}")
                    .SendAsync("KanbanTicketUpdated", new
                    {
                        TicketId = ticketId,
                        Data = ticketData,
                        UpdatedBy = updatedBy,
                        Timestamp = DateTime.UtcNow
                    });
            }

            _logger.LogDebug("Sent ticket update notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket update notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task NotifyTicketAssignedAsync(int ticketId, object assignmentData)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("TicketAssigned", new
                {
                    TicketId = ticketId,
                    Data = assignmentData,
                    Timestamp = DateTime.UtcNow
                });

            // Also notify the assigned user specifically
            var assignedUserId = GetAssignedUserIdFromData(assignmentData);
            if (!string.IsNullOrEmpty(assignedUserId))
            {
                await _hubContext.Clients.User(assignedUserId)
                    .SendAsync("TicketAssignedToYou", new
                    {
                        TicketId = ticketId,
                        Data = assignmentData,
                        Timestamp = DateTime.UtcNow
                    });
            }

            _logger.LogDebug("Sent ticket assignment notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket assignment notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task NotifyTicketStatusChangedAsync(int ticketId, object statusData)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("TicketStatusChanged", new
                {
                    TicketId = ticketId,
                    Data = statusData,
                    Timestamp = DateTime.UtcNow
                });

            // Update kanban board
            var tenantId = GetTenantIdFromTicketData(statusData);
            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"kanban_{tenantId.Value}")
                    .SendAsync("KanbanStatusChanged", new
                    {
                        TicketId = ticketId,
                        Data = statusData,
                        Timestamp = DateTime.UtcNow
                    });
            }

            _logger.LogDebug("Sent ticket status change notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket status change notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task NotifyNewMessageAsync(int ticketId, object messageData)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("NewMessage", new
                {
                    TicketId = ticketId,
                    Data = messageData,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent new message notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send new message notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task NotifyUserTypingAsync(int ticketId, string userId, bool isTyping)
    {
        try
        {
            await _hubContext.Clients.GroupExcept($"ticket_{ticketId}", await GetConnectionsForUser(userId))
                .SendAsync(isTyping ? "UserTyping" : "UserStoppedTyping", new
                {
                    TicketId = ticketId,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent typing notification for user {UserId} in ticket {TicketId}", userId, ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing notification for user {UserId} in ticket {TicketId}", userId, ticketId);
        }
    }

    public async Task NotifyKanbanUpdatedAsync(int tenantId, object kanbanData)
    {
        try
        {
            await _hubContext.Clients.Group($"kanban_{tenantId}")
                .SendAsync("KanbanUpdated", new
                {
                    Data = kanbanData,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent kanban update notification for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send kanban update notification for tenant {TenantId}", tenantId);
        }
    }

    public async Task NotifySlaBreachAsync(int ticketId, object slaData)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("SlaBreach", new
                {
                    TicketId = ticketId,
                    Data = slaData,
                    Timestamp = DateTime.UtcNow,
                    Urgency = "high"
                });

            // Also notify admin roles
            var tenantId = GetTenantIdFromTicketData(slaData);
            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"role_Admin")
                    .SendAsync("SlaBreachAlert", new
                    {
                        TicketId = ticketId,
                        Data = slaData,
                        Timestamp = DateTime.UtcNow,
                        Urgency = "high"
                    });

                await _hubContext.Clients.Group($"role_SuperAdmin")
                    .SendAsync("SlaBreachAlert", new
                    {
                        TicketId = ticketId,
                        Data = slaData,
                        Timestamp = DateTime.UtcNow,
                        Urgency = "high"
                    });
            }

            _logger.LogWarning("Sent SLA breach notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SLA breach notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task NotifyWorkflowExecutedAsync(int ticketId, object workflowData)
    {
        try
        {
            await _hubContext.Clients.Group($"ticket_{ticketId}")
                .SendAsync("WorkflowExecuted", new
                {
                    TicketId = ticketId,
                    Data = workflowData,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent workflow execution notification for ticket {TicketId}", ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send workflow execution notification for ticket {TicketId}", ticketId);
        }
    }

    public async Task SendNotificationToUserAsync(string userId, object notificationData)
    {
        try
        {
            await _hubContext.Clients.User(userId)
                .SendAsync("Notification", new
                {
                    Data = notificationData,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task SendNotificationToRoleAsync(int tenantId, string role, object notificationData)
    {
        try
        {
            await _hubContext.Clients.Group($"role_{role}")
                .SendAsync("Notification", new
                {
                    Data = notificationData,
                    TargetRole = role,
                    TenantId = tenantId,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent notification to role {Role} in tenant {TenantId}", role, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to role {Role} in tenant {TenantId}", role, tenantId);
        }
    }

    public async Task SendNotificationToTenantAsync(int tenantId, object notificationData)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant_{tenantId}")
                .SendAsync("TenantNotification", new
                {
                    Data = notificationData,
                    TenantId = tenantId,
                    Timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("Sent tenant-wide notification for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tenant-wide notification for tenant {TenantId}", tenantId);
        }
    }

    private int? GetTenantIdFromTicketData(object ticketData)
    {
        try
        {
            var json = JsonSerializer.Serialize(ticketData);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (data?.TryGetValue("TenantId", out var tenantIdValue) == true)
            {
                if (int.TryParse(tenantIdValue.ToString(), out var tenantId))
                    return tenantId;
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return null;
    }

    private string? GetAssignedUserIdFromData(object assignmentData)
    {
        try
        {
            var json = JsonSerializer.Serialize(assignmentData);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (data?.TryGetValue("AssignedToUserId", out var userIdValue) == true)
            {
                return userIdValue.ToString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return null;
    }

    private async Task<IReadOnlyList<string>> GetConnectionsForUser(string userId)
    {
        var connections = await _connectionManager.GetConnectionsForUserAsync(userId);
        return connections.ToList().AsReadOnly();
    }
}
