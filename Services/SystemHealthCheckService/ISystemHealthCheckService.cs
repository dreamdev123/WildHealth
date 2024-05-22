namespace WildHealth.Application.Services.SystemHealthCheckService
{
    /// <summary>
    /// Provides quick smoke tests for functionality and connectivity in the application.
    /// </summary>
    public interface ISystemHealthCheckService 
    {
        bool CheckAll();

        bool CheckSql();
        bool CheckCache(); 
        void LogThreadInfo(); 
    }
}