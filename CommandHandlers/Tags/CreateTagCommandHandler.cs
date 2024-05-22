using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Services.Tags;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.CommandHandlers.Tags;

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand>
{
    private readonly IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> _cacheService;
    private readonly ITagRelationsService _tagRelationsService;
    
    private static readonly string[] InsuranceRelatedTags = {
        Common.Constants.Tags.InsurancePending,
        Common.Constants.Tags.InsuranceNotVerified,
        Common.Constants.Tags.InsuranceVerified
    };

    public CreateTagCommandHandler(
        IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> cacheService,
            ITagRelationsService tagRelationsService)
    {
        _cacheService = cacheService;
        _tagRelationsService = tagRelationsService;
    }

    public async Task Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        //there can be only one insurance-related tag per patient
        if (InsuranceRelatedTags.Contains(request.Tag))
        {
            await DeleteExistingInsuranceTagsForPatient(request.Patient);
        }
        
        await _tagRelationsService.GetOrCreate(request.Patient, request.Tag);

        _cacheService.RemoveKey(request.Patient.Id.GetHashCode().ToString());
    }

    private async Task DeleteExistingInsuranceTagsForPatient(Patient patient)
    {
        foreach (var tag in InsuranceRelatedTags)
        {
            var tagRelation = await _tagRelationsService.Get(patient, tag);
            
            if (tagRelation is not null)
            {
                await _tagRelationsService.Delete(tagRelation);
            }
        }
    }
}