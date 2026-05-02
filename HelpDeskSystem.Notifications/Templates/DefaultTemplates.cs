using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Notifications.Templates;

public static class DefaultTemplates
{
    public static List<EmailTemplate> GetDefaultTemplates(int tenantId)
    {
        return new List<EmailTemplate>
        {
            new EmailTemplate
            {
                Name = "Ticket Created",
                TemplateType = "ticket_created",
                Subject = "New Ticket Created: {{TicketNumber}} - {{TicketTitle}}",
                HtmlBody = GetTicketCreatedHtmlTemplate(),
                TextBody = GetTicketCreatedTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            },
            new EmailTemplate
            {
                Name = "Ticket Assigned",
                TemplateType = "ticket_assigned",
                Subject = "Ticket {{TicketNumber}} Assigned to You",
                HtmlBody = GetTicketAssignedHtmlTemplate(),
                TextBody = GetTicketAssignedTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            },
            new EmailTemplate
            {
                Name = "Ticket Status Changed",
                TemplateType = "ticket_status_changed",
                Subject = "Ticket {{TicketNumber}} Status Changed to {{NewStatus}}",
                HtmlBody = GetTicketStatusChangedHtmlTemplate(),
                TextBody = GetTicketStatusChangedTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            },
            new EmailTemplate
            {
                Name = "SLA Breach",
                TemplateType = "sla_breach",
                Subject = "URGENT: SLA Breach for Ticket {{TicketNumber}}",
                HtmlBody = GetSlaBreachHtmlTemplate(),
                TextBody = GetSlaBreachTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            },
            new EmailTemplate
            {
                Name = "Ticket Resolved",
                TemplateType = "ticket_resolved",
                Subject = "Ticket {{TicketNumber}} Has Been Resolved",
                HtmlBody = GetTicketResolvedHtmlTemplate(),
                TextBody = GetTicketResolvedTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            },
            new EmailTemplate
            {
                Name = "Ticket Escalated",
                TemplateType = "ticket_escalated",
                Subject = "Ticket {{TicketNumber}} Has Been Escalated",
                HtmlBody = GetTicketEscalatedHtmlTemplate(),
                TextBody = GetTicketEscalatedTextTemplate(),
                TenantId = tenantId,
                IsActive = true
            }
        };
    }

    private static string GetTicketCreatedHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>New Ticket Created</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #007bff; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #007bff; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>New Ticket Created</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            <p>A new ticket has been created and requires your attention:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Ticket Number:</strong> {{TicketNumber}}</p>
                <p><strong>Priority:</strong> {{Priority}}</p>
                <p><strong>Category:</strong> {{Category}}</p>
                <p><strong>Created By:</strong> {{CreatedBy}}</p>
                <p><strong>Description:</strong></p>
                <p>{{Description}}</p>
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>View Ticket</a>
            </p>
            
            <p>Please review and take appropriate action.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetTicketCreatedTextTemplate()
    {
        return @"
NEW TICKET CREATED

Hello {{UserName}},

A new ticket has been created and requires your attention:

Ticket Details:
- Ticket Number: {{TicketNumber}}
- Title: {{TicketTitle}}
- Priority: {{Priority}}
- Category: {{Category}}
- Created By: {{CreatedBy}}
- Description: {{Description}}

Please review and take appropriate action.

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }

    private static string GetTicketAssignedHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Ticket Assigned</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #28a745; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #28a745; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Ticket Assigned to You</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            <p>Ticket <strong>{{TicketNumber}}</strong> has been assigned to you:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Assigned By:</strong> {{AssignedBy}}</p>
                <p><strong>Priority:</strong> {{Priority}}</p>
                <p><strong>Category:</strong> {{Category}}</p>
                <p><strong>Status:</strong> {{Status}}</p>
                {{#if AssignmentReason}}
                <p><strong>Assignment Reason:</strong> {{AssignmentReason}}</p>
                {{/if}}
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>View Ticket</a>
            </p>
            
            <p>Please take ownership and start working on this ticket.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetTicketAssignedTextTemplate()
    {
        return @"
TICKET ASSIGNED TO YOU

Hello {{UserName}},

Ticket {{TicketNumber}} has been assigned to you:

Ticket Details:
- Title: {{TicketTitle}}
- Assigned By: {{AssignedBy}}
- Priority: {{Priority}}
- Category: {{Category}}
- Status: {{Status}}
{{#if AssignmentReason}}
- Assignment Reason: {{AssignmentReason}}
{{/if}}

Please take ownership and start working on this ticket.

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }

    private static string GetTicketStatusChangedHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Ticket Status Changed</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ffc107; color: #333; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #ffc107; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #ffc107; color: #333; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Ticket Status Changed</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            <p>The status of ticket <strong>{{TicketNumber}}</strong> has been changed:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Old Status:</strong> {{OldStatus}}</p>
                <p><strong>New Status:</strong> {{NewStatus}}</p>
                <p><strong>Changed By:</strong> {{ChangedBy}}</p>
                {{#if Comment}}
                <p><strong>Comment:</strong> {{Comment}}</p>
                {{/if}}
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>View Ticket</a>
            </p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetTicketStatusChangedTextTemplate()
    {
        return @"
TICKET STATUS CHANGED

Hello {{UserName}},

The status of ticket {{TicketNumber}} has been changed:

Ticket Details:
- Title: {{TicketTitle}}
- Old Status: {{OldStatus}}
- New Status: {{NewStatus}}
- Changed By: {{ChangedBy}}
{{#if Comment}}
- Comment: {{Comment}}
{{/if}}

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }

    private static string GetSlaBreachHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>SLA BREACH - URGENT</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #dc3545; margin: 10px 0; }
        .warning { background: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #dc3545; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ URGENT: SLA BREACH</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            
            <div class='warning'>
                <strong>⚠️ WARNING:</strong> Ticket {{TicketNumber}} has breached its Service Level Agreement (SLA)!
            </div>
            
            <p>The following ticket has exceeded its SLA limits:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Ticket Number:</strong> {{TicketNumber}}</p>
                <p><strong>Priority:</strong> {{Priority}}</p>
                <p><strong>Category:</strong> {{Category}}</p>
                <p><strong>Current Status:</strong> {{Status}}</p>
                <p><strong>SLA Type:</strong> {{SlaType}}</p>
                <p><strong>SLA Deadline:</strong> {{SlaDeadline}}</p>
                <p><strong>Breached At:</strong> {{BreachedAt}}</p>
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>Take Immediate Action</a>
            </p>
            
            <p><strong>Immediate action required!</strong> Please address this ticket immediately to restore service levels.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetSlaBreachTextTemplate()
    {
        return @"
⚠️ URGENT: SLA BREACH

Hello {{UserName}},

⚠️ WARNING: Ticket {{TicketNumber}} has breached its Service Level Agreement (SLA)!

The following ticket has exceeded its SLA limits:

Ticket Details:
- Ticket Number: {{TicketNumber}}
- Title: {{TicketTitle}}
- Priority: {{Priority}}
- Category: {{Category}}
- Current Status: {{Status}}
- SLA Type: {{SlaType}}
- SLA Deadline: {{SlaDeadline}}
- Breached At: {{BreachedAt}}

IMMEDIATE ACTION REQUIRED!
Please address this ticket immediately to restore service levels.

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }

    private static string GetTicketResolvedHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Ticket Resolved</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #28a745; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #28a745; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Ticket Resolved</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            <p>Good news! Ticket <strong>{{TicketNumber}}</strong> has been resolved:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Resolved By:</strong> {{ResolvedBy}}</p>
                <p><strong>Resolution Time:</strong> {{ResolutionTime}}</p>
                {{#if ResolutionComment}}
                <p><strong>Resolution Comment:</strong> {{ResolutionComment}}</p>
                {{/if}}
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>View Resolution</a>
            </p>
            
            <p>Please review the resolution and let us know if you need any further assistance.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetTicketResolvedTextTemplate()
    {
        return @"
✅ TICKET RESOLVED

Hello {{UserName}},

Good news! Ticket {{TicketNumber}} has been resolved:

Ticket Details:
- Title: {{TicketTitle}}
- Resolved By: {{ResolvedBy}}
- Resolution Time: {{ResolutionTime}}
{{#if ResolutionComment}}
- Resolution Comment: {{ResolutionComment}}
{{/if}}

Please review the resolution and let us know if you need any further assistance.

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }

    private static string GetTicketEscalatedHtmlTemplate()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Ticket Escalated</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #fd7e14; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f9f9f9; }
        .ticket-info { background: white; padding: 15px; border-left: 4px solid #fd7e14; margin: 10px 0; }
        .escalation { background: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin: 10px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .btn { display: inline-block; padding: 10px 20px; background: #fd7e14; color: white; text-decoration: none; border-radius: 4px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔥 Ticket Escalated</h1>
        </div>
        <div class='content'>
            <p>Hello {{UserName}},</p>
            
            <div class='escalation'>
                <strong>🔥 ESCALATION:</strong> Ticket {{TicketNumber}} has been escalated to {{EscalatedToRole}}.
            </div>
            
            <p>The following ticket has been escalated and requires your immediate attention:</p>
            
            <div class='ticket-info'>
                <h3>{{TicketTitle}}</h3>
                <p><strong>Ticket Number:</strong> {{TicketNumber}}</p>
                <p><strong>Escalated By:</strong> {{EscalatedBy}}</p>
                <p><strong>Escalated To:</strong> {{EscalatedToRole}}</p>
                <p><strong>Priority:</strong> {{Priority}}</p>
                <p><strong>Category:</strong> {{Category}}</p>
                <p><strong>Escalation Reason:</strong> {{EscalationReason}}</p>
            </div>
            
            <p>
                <a href='{{TicketUrl}}' class='btn'>Handle Escalation</a>
            </p>
            
            <p>This ticket requires immediate attention from the {{EscalatedToRole}} team.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {{TenantName}} Help Desk System</p>
            <p>Date: {{CurrentDate}}</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetTicketEscalatedTextTemplate()
    {
        return @"
🔥 TICKET ESCALATED

Hello {{UserName}},

🔥 ESCALATION: Ticket {{TicketNumber}} has been escalated to {{EscalatedToRole}}.

The following ticket has been escalated and requires your immediate attention:

Ticket Details:
- Ticket Number: {{TicketNumber}}
- Title: {{TicketTitle}}
- Escalated By: {{EscalatedBy}}
- Escalated To: {{EscalatedToRole}}
- Priority: {{Priority}}
- Category: {{Category}}
- Escalation Reason: {{EscalationReason}}

This ticket requires immediate attention from the {{EscalatedToRole}} team.

---
This email was sent by {{TenantName}} Help Desk System
Date: {{CurrentDate}}
";
    }
}
