namespace HelpDeskSystem.Application.Interfaces;

public interface IReportingService
{
    Task<FullReportDto> GenerateFullReportAsync(ReportRequestDto request);
    Task<IEnumerable<TimeSeriesPointDto>> GetTicketVolumeTimeSeriesAsync(int? tenantId, DateTime from, DateTime to, string groupBy = "day");
    Task<IEnumerable<AgentLeaderboardDto>> GetAgentLeaderboardAsync(int? tenantId, DateTime from, DateTime to);
    Task<SlaBreachReportDto> GetSlaBreachReportAsync(int? tenantId, DateTime from, DateTime to);
    Task<FirstResponseReportDto> GetFirstResponseReportAsync(int? tenantId, DateTime from, DateTime to);
    Task<ResolutionTimeReportDto> GetResolutionTimeReportAsync(int? tenantId, DateTime from, DateTime to);
    Task<CsatReportDto> GetCsatReportAsync(int? tenantId, DateTime from, DateTime to);
    Task<ChannelReportDto> GetChannelBreakdownAsync(int? tenantId, DateTime from, DateTime to);
    Task<byte[]> ExportReportToCsvAsync(ReportRequestDto request);
    Task<IEnumerable<BacklogReportDto>> GetBacklogTrendAsync(int? tenantId, int days = 30);
}

public record ReportRequestDto(
    int? TenantId,
    DateTime From,
    DateTime To,
    string GroupBy = "day",
    string? AgentId = null,
    int? CategoryId = null,
    int? PriorityId = null
);

public record FullReportDto(
    DateTime GeneratedAt,
    int TotalTickets,
    int ResolvedTickets,
    int OpenTickets,
    int BreachedSlaCount,
    double SlaComplianceRate,
    double AverageResolutionHours,
    double AverageFirstResponseHours,
    double CsatScore,
    IEnumerable<TimeSeriesPointDto> VolumeTrend,
    IEnumerable<AgentLeaderboardDto> TopAgents,
    IEnumerable<CategoryBreakdownDto> CategoryBreakdown
);

public record TimeSeriesPointDto(DateTime Date, int Count, string? Label = null);

public record AgentLeaderboardDto(
    int UserId,
    string AgentName,
    int TicketsResolved,
    double AvgResolutionHours,
    double AvgFirstResponseHours,
    double CsatScore,
    int SlaBreaches
);

public record SlaBreachReportDto(
    int TotalBreaches,
    double BreachRate,
    IEnumerable<SlaBreachItemDto> Breaches
);

public record SlaBreachItemDto(int TicketId, string TicketNumber, string Title, DateTime DueAt, DateTime BreachedAt, string? AgentName, string Priority);

public record FirstResponseReportDto(
    double AverageMinutes,
    double MedianMinutes,
    double WithinSlaPercentage,
    IEnumerable<TimeSeriesPointDto> Trend
);

public record ResolutionTimeReportDto(
    double AverageHours,
    double MedianHours,
    IEnumerable<TimeSeriesPointDto> Trend,
    IEnumerable<CategoryBreakdownDto> ByCategory
);

public record CategoryBreakdownDto(string CategoryName, int Count, double Percentage, double AvgResolutionHours);

public record CsatReportDto(
    double AverageScore,
    int Responses,
    double ResponseRate,
    IEnumerable<TimeSeriesPointDto> Trend,
    IEnumerable<CsatCommentDto> RecentComments
);

public record CsatCommentDto(int TicketId, string TicketNumber, int Rating, string? Comment, DateTime Date, string? AgentName);

public record ChannelReportDto(IEnumerable<ChannelBreakdownItemDto> Channels);
public record ChannelBreakdownItemDto(string Channel, int Count, double Percentage, double AvgResolutionHours);

public record BacklogReportDto(DateTime Date, int OpenCount, int InProgressCount);
