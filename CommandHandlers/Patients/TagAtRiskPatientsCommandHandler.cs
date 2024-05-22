using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Tags;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Tags;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class TagAtRiskPatientsCommandHandler : IRequestHandler<TagAtRiskPatientsCommand>
    {
        private readonly string _atRiskIccTagName = TagsConstants.AtICCRisk;
        private readonly string _atRiskImcTagName = TagsConstants.AtIMCRisk;
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;
        private readonly ITagRelationsService _tagRelationsService;
        private readonly IGeneralRepository<TagRelation> _tagRelationsRepository;

        public TagAtRiskPatientsCommandHandler(
            IPatientsService patientsService,
            ILogger<TagAtRiskPatientsCommandHandler> logger,
            ITagsService tagsService,
            ITagRelationsService tagRelationsService,
            IGeneralRepository<TagRelation> tagRelationsRepository
            )
        {
            _patientsService = patientsService;
            _logger = logger;
            _tagRelationsService = tagRelationsService;
            _tagRelationsRepository = tagRelationsRepository;
        }

        public async Task Handle(TagAtRiskPatientsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting tagging of at risk patients");

            try
            {
                var patientsWithAtRiskIccTag = (await _tagRelationsService.GetAllOfTag(_atRiskIccTagName));

                var patientsWithAtRiskImcTag = (await _tagRelationsService.GetAllOfTag(_atRiskImcTagName));

                var iccRiskPatients = await _patientsService.AtRiskIccDue();

                var imcRiskPatients = await _patientsService.AtRiskImcDue();

                var iccRiskUniversalIds = iccRiskPatients.Select(o => o.UniversalId).ToArray();
                
                var imcRiskUniversalIds = imcRiskPatients.Select(o => o.UniversalId).ToArray();
                

                foreach (var item in iccRiskPatients)
                {
                    var patient = await _patientsService.GetByIdAsync(item.PatientId);
                
                    await _tagRelationsService.GetOrCreate(
                        taggable: patient,
                        name: _atRiskIccTagName
                    );
                }

                foreach (var item in imcRiskPatients)
                {
                    var patient = await _patientsService.GetByIdAsync(item.PatientId);
                    
                    await _tagRelationsService.GetOrCreate(
                        taggable: patient,
                        name: _atRiskImcTagName
                    );
                }
                
                // For all patients that are no longer at risk, make sure we remove the tag
                // This should be getting removed as appointments get scheduled and are performed, however this is a failsafe
                var iccTagRelationsToRemove = patientsWithAtRiskIccTag.Where(o => !iccRiskUniversalIds.Contains(o.UniqueGuid))
                    .ToArray();
                
                foreach (var iccTagRelationToRemove in iccTagRelationsToRemove)
                {
                    _tagRelationsRepository.Delete(iccTagRelationToRemove);

                    await _tagRelationsRepository.SaveAsync();
                }
                
                // For all patients that are no longer at risk, make sure we remove the tag
                // This should be getting removed as appointments get scheduled and are performed, however this is a failsafe
                var imcTagRelationsToRemove = patientsWithAtRiskImcTag
                    .Where(o => !imcRiskUniversalIds.Contains(o.UniqueGuid)).ToArray();
                
                foreach (var imcTagRelationToRemove in imcTagRelationsToRemove)
                {
                    _tagRelationsRepository.Delete(imcTagRelationToRemove);

                    await _tagRelationsRepository.SaveAsync();
                }
                
                _logger.LogInformation($"Finished tagging of at risk patients");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run the at risk tagging command - {ex}");
                
            }
        }

    }
}
