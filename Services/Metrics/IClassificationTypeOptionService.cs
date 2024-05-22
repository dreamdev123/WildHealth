using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Services.Metrics
{
    public interface IClassificationTypeOptionService
    {
        public Task<ClassificationTypeOption> GetByTypeAndOptionName(ClassificationType type, string optionName);
        public Task<IDictionary<ClassificationType, IList<ClassificationTypeOption>>> GetAllByTypeAsync();
    }
}