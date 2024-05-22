using System.Threading;
using System.Threading.Tasks;

namespace WildHealth.Application.Utils.MultiThreading;

public interface IRunInParallelUtility<T, T1>
{
    delegate Task<T[]> SourceDelegate(T1 command, int? maxRecords);
    delegate Task ExecuteDelegate(T[] records, T1 command, CancellationToken token);

    Task Run(
        int shardSize,
        SourceDelegate sourceFunction,
        ExecuteDelegate executeFunction,
        T1 command,
        int? maxRecords);
}