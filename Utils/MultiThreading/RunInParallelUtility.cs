using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Extensions;

namespace WildHealth.Application.Utils.MultiThreading;

public class RunInParallelUtility<T, T1> : IRunInParallelUtility<T, T1>
{
    
    private readonly ILogger<RunInParallelUtility<T, T1>> _logger;

    public RunInParallelUtility(ILogger<RunInParallelUtility<T, T1>> logger)
    {
        _logger = logger;
    }

    public async Task Run(
        int shardSize,
        IRunInParallelUtility<T, T1>.SourceDelegate sourceFunction,
        IRunInParallelUtility<T, T1>.ExecuteDelegate executeFunction,
        T1 command,
        int? maxRecords)
    {
        _logger.LogInformation($"Parallelization of [ShardSize] = {shardSize}, [Type] = {typeof(T).FullName}: started");

        var records = await sourceFunction(command, maxRecords);

        if (records.IsNullOrEmpty())
        {
            _logger.LogInformation($"Parallelization of [ShardSize] = {shardSize}, [Type] = {typeof(T).FullName}: failed to find any records");
            return;
        }

        var numberOfThreads = (records.Length / shardSize) + ((records.Length % shardSize) > 0 ? 1 : 0);

        var splitNumber = records.Length / numberOfThreads;

        var shards = records.Split(splitNumber).ToArray();

        await Parallel.ForEachAsync(shards, async (shard, token) =>
        {
            var enumerable = shard as T[] ?? shard.ToArray();
            
            if (enumerable.Any())
            {
                await executeFunction(enumerable.ToArray(), command, token);
            }
        });
        
        _logger.LogInformation($"Parallelization of [ShardSize] = {shardSize}, [Type] = {typeof(T).FullName}: started");
    }
}