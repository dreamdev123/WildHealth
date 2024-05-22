using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Metrics
{
    public class ClassificationTypeOptionService : IClassificationTypeOptionService
    {
        private readonly IGeneralRepository<ClassificationTypeOption> _classificationTypeRepository;

        public ClassificationTypeOptionService(
            IGeneralRepository<ClassificationTypeOption> classificationTypeRepository
        )
        {
            _classificationTypeRepository = classificationTypeRepository;
        }

        public async Task<ClassificationTypeOption> GetByTypeAndOptionName(ClassificationType classificationType, string optionName)
        {
            var option = await _classificationTypeRepository
                .All()
                .ByClassificationType(classificationType)
                .ByName(optionName)
                .FirstOrDefaultAsync();

            if (option is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"No ClassificationTypeOption found for type {classificationType.Id} and name {optionName}");
            }

            return option;
        }

        public async Task<IDictionary<ClassificationType, IList<ClassificationTypeOption>>> GetAllByTypeAsync()
        {
            var options = await _classificationTypeRepository
                .All()
                .Include(x => x.ClassificationType)
                .ToListAsync();

            var optionsDict = new Dictionary<ClassificationType, IList<ClassificationTypeOption>>();

            foreach (var option in options)
            {
                if (!optionsDict.Keys.Contains(option.ClassificationType)) 
                {
                    optionsDict[option.ClassificationType] = new List<ClassificationTypeOption>();
                }
                
                optionsDict[option.ClassificationType].Add(option);
            }

            return optionsDict;
        }
    }
}