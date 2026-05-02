namespace HelpDeskSystem.Shared.Helpers;

public static class TicketNumberGenerator
{
    private static int _counter = 0;

    public static string Generate()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var counter = Interlocked.Increment(ref _counter);
        return $"TCK-{timestamp}-{counter:D4}";
    }
}