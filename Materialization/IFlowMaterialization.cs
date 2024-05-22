using System.Threading.Tasks;
using WildHealth.Application.Functional.Flow;

namespace WildHealth.Application.Materialization;

public interface IFlowMaterialization
{
    Task Materialize(MaterialisableFlowResult source);
}