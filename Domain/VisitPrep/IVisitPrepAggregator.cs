using System.Threading.Tasks;
using WildHealth.Common.Models.VisitPrep;

namespace WildHealth.Application.Domain.VisitPrep;

public interface IVisitPrepAggregator
{
    Task<VisitPrepModel> GetAsync(int patientId);
}