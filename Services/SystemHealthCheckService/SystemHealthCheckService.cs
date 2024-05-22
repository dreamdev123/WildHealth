using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Common.Models.SystemHealthCheck;
using WildHealth.Shared.Data.Context;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.Services.SystemHealthCheckService
{
    public class SystemHealthCheckService : ISystemHealthCheckService 
    {
        private ILogger<SystemHealthCheckService> Logger { get; }
        private IApplicationDbContext DbContext { get; }
        private IWildHealthCacheService<SystemHealthCheckService> WildHealthCacheService { get; }

        public SystemHealthCheckService(IApplicationDbContext dbContext,
                                        IWildHealthCacheService<SystemHealthCheckService> wildHealthCacheService,
                                        ILogger<SystemHealthCheckService> logger
                                        )
        { 
            Logger = logger;
            DbContext = dbContext;
            WildHealthCacheService = wildHealthCacheService;
        }

        public bool CheckAll()
        {
            Logger.LogInformation("Health checking...");
            var cacheOk = CheckCache();
            var dbOk = CheckSql();
            Logger.LogInformation("Health check complete.");
            return cacheOk && dbOk;
        }

        public void LogThreadInfo()
        {
            try
            {
                var ts = GetThreads().OrderBy(t => t.Id);
                var data = Newtonsoft.Json.JsonConvert.SerializeObject(ts, Formatting.Indented);
                Logger.LogInformation($"\n{data}");
            }
            catch (Exception e)
            {
                Logger.LogError($"There is a problem loading the thread info: {e}");
            }
        }

        public bool CheckCache()
        {
            var dt = DateTime.UtcNow.ToString();
            try {
                WildHealthCacheService.SetString("LastHealthCheckUTC", dt);
                var readback = WildHealthCacheService.GetString("LastHealthCheckUTC");
                Logger.LogInformation($"Cache ok: {readback}");
                return true;
            } 
            catch (Exception e) 
            {
                Logger.LogError($"Health check failed for cache service: {e.Message}");
                return false;
            }
        }

        public bool CheckSql()
        {
            try 
            {
                using (var command = DbContext.Instance.Database.GetDbConnection().CreateCommand()) 
                {
                    command.CommandText = "select current_timestamp";
                    command.Connection!.Open();
                    var dt = command.ExecuteScalar();
                    command.Connection!.Close();
                    Logger.LogInformation($"Sql ok: {dt}");
                }
                return true;
            } 
            catch (Exception e) 
            {
                Logger.LogError($"Health check failed for sql database: {e.Message}");
                return false;
            }
        }

        
        private IEnumerable<ThreadModel> GetThreads()
        {
            var result = new List<ThreadModel>();
            var ts = Process.GetCurrentProcess().Threads;
            for (int i = 0; i < ts.Count; i++)
            {
                try
                {
                    var thread = ts[i];
                    var startTime = TryGetStartTime(thread);
                    var m = new ThreadModel()
                    {
                        Id = thread.Id,
                        StartTime = startTime,
                        TotalProcessorTime = thread.TotalProcessorTime,
                        State = thread.ThreadState,
                        StartAddress = thread.StartAddress
                    };
                    result.Add(m);
                }
                catch 
                { 
                    //fine.
                    //this can happen for a variety of reasons, like if the thread exits while we
                    //build the report, or a property isn't supported on a particular platform
                }
            }
            return result;
        }

        private DateTime TryGetStartTime(ProcessThread thread)
        {
            try
            {
                //This operation is not supported on osx.
                return thread.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }

}