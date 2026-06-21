namespace PROJECT2106.Services;

public interface IActivityLogService
{
    void Log(string username, string action);
    List<string> GetLogs();
}