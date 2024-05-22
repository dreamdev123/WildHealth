using System.Threading.Tasks;
using WildHealth.Domain.Entities.Actions;

namespace WildHealth.Application.Services.CallToActions;

public interface ICallToActionsService
{
    Task<CallToAction> GetAsync(int id);
    
    Task<CallToAction[]> ActiveAsync(int patientId);
    
    Task<CallToAction[]> AllAsync(int patientId);
}