namespace HelpDeskSystem.Domain.Enums;

public enum IdentityProtocol
{
    Oidc = 1,
    Saml = 2
}

public enum PolicyEffect
{
    Deny = 1,
    Allow = 2
}

public enum ChannelType
{
    Email = 1,
    Chat = 2,
    WhatsApp = 3,
    Social = 4,
    Voice = 5,
    WebForm = 6
}

public enum ConnectorStatus
{
    Disabled = 1,
    Enabled = 2,
    Error = 3
}

public enum InboundEventStatus
{
    Received = 1,
    Normalized = 2,
    TicketCreated = 3,
    Failed = 4
}

public enum DataSubjectRequestType
{
    Export = 1,
    Delete = 2,
    Rectify = 3
}

public enum DataSubjectRequestStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Rejected = 4
}

public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    PastDue = 3,
    Suspended = 4,
    Cancelled = 5
}

public enum AppInstallStatus
{
    Pending = 1,
    Installed = 2,
    Failed = 3,
    Disabled = 4
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Void = 4,
    Overdue = 5
}

public enum DsrProcessStage
{
    Intake = 1,
    IdentityVerification = 2,
    DataDiscovery = 3,
    Fulfillment = 4,
    Closed = 5
}

public enum OutboundMessageStatus
{
    Pending = 1,
    Sending = 2,
    Delivered = 3,
    Failed = 4,
    Retrying = 5,
    Cancelled = 6
}

public enum DeliveryReceiptStatus
{
    Accepted = 1,
    Sent = 2,
    Delivered = 3,
    Read = 4,
    Failed = 5
}

public enum TenantFailoverMode
{
    Manual = 1,
    Automatic = 2
}
