using System.Linq;
using System.Threading.Tasks;

namespace WildHealth.Application.Functional.Flow;

public interface IFlow<out TResult>
{
    TResult Execute();
}

public interface IQueryFlow<TResult> : IFlow<IQueryable<TResult>> {}

public interface IMaterialisableFlow : IFlow<MaterialisableFlowResult> {}
