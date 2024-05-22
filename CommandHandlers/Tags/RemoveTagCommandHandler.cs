using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Services.Tags;
using WildHealth.Common.Models.Patients;
using WildHealth.Shared.DistributedCache.Services;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Tags;

public class RemoveTagCommandHandler : IRequestHandler<RemoveTagCommand>
{
    private readonly IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> _cacheService;
    private readonly ITagRelationsService _tagRelationsService;
    
    public RemoveTagCommandHandler(
        IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> cacheService,
        ITagRelationsService tagRelationsService)
    {
        _cacheService = cacheService;
        _tagRelationsService = tagRelationsService;
    }
    
    public async Task Handle(RemoveTagCommand command, CancellationToken cancellationToken)
    {
        var tagRelation = await _tagRelationsService.Get(command.Patient, command.Tag);
        
        if (tagRelation != null)
        {
            await _tagRelationsService.Delete(tagRelation);
            
            _cacheService.RemoveKey(command.Patient.GetId().GetHashCode().ToString());
        }
    }
}