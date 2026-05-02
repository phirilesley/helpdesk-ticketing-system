using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Application.Services;

public class BusinessTimeService : IBusinessTimeService
{
    private readonly HelpDeskDbContext _context;

    public BusinessTimeService(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<DateTime> AddBusinessMinutesAsync(
        int tenantId,
        DateTime startUtc,
        int minutesToAdd,
        CancellationToken cancellationToken = default)
    {
        if (minutesToAdd <= 0)
            return startUtc;

        var profile = await _context.BusinessHoursProfiles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
            return startUtc.AddMinutes(minutesToAdd);

        var timezone = ResolveTimeZone(profile.TimeZoneId);
        var workingDays = ParseWorkingDays(profile.WorkingDays);
        if (workingDays.Count == 0)
            return startUtc.AddMinutes(minutesToAdd);

        var local = TimeZoneInfo.ConvertTimeFromUtc(startUtc, timezone);
        local = NormalizeToWorkWindow(local, profile, workingDays);

        var remaining = minutesToAdd;
        while (remaining > 0)
        {
            var dayEnd = local.Date.Add(profile.EndLocalTime.ToTimeSpan());
            if (local >= dayEnd)
            {
                local = NextWorkdayStart(local, profile, workingDays);
                continue;
            }

            var available = (int)Math.Floor((dayEnd - local).TotalMinutes);
            if (available <= 0)
            {
                local = NextWorkdayStart(local, profile, workingDays);
                continue;
            }

            if (remaining <= available)
            {
                local = local.AddMinutes(remaining);
                remaining = 0;
            }
            else
            {
                local = NextWorkdayStart(local, profile, workingDays);
                remaining -= available;
            }
        }

        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timezone);
    }

    private static TimeZoneInfo ResolveTimeZone(string timezoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static HashSet<int> ParseWorkingDays(string value)
    {
        var set = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(value))
            return set;

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var day) && day >= 0 && day <= 6)
                set.Add(day);
        }
        return set;
    }

    private static DateTime NormalizeToWorkWindow(DateTime local, BusinessHoursProfile profile, HashSet<int> workingDays)
    {
        if (!workingDays.Contains((int)local.DayOfWeek))
            return NextWorkdayStart(local, profile, workingDays);

        var dayStart = local.Date.Add(profile.StartLocalTime.ToTimeSpan());
        var dayEnd = local.Date.Add(profile.EndLocalTime.ToTimeSpan());

        if (local < dayStart)
            return dayStart;
        if (local >= dayEnd)
            return NextWorkdayStart(local, profile, workingDays);
        return local;
    }

    private static DateTime NextWorkdayStart(DateTime local, BusinessHoursProfile profile, HashSet<int> workingDays)
    {
        var probe = local.Date.AddDays(1);
        while (!workingDays.Contains((int)probe.DayOfWeek))
        {
            probe = probe.AddDays(1);
        }
        return probe.Add(profile.StartLocalTime.ToTimeSpan());
    }
}
