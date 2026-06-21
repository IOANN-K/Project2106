namespace PROJECT2106.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly List<string> _logs = new();
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(ILogger<ActivityLogService> logger)
    {
        _logger = logger;
    }

    public void Log(string username, string action)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {username}: {action}";
        _logs.Add(entry);
        _logger.LogInformation(entry);
    }

    public List<string> GetLogs()
    {
        return _logs;
    }
}