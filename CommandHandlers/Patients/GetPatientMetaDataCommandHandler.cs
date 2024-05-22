using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit.Initializers;
using MediatR;
using WildHealth.Application.CommandHandlers.Patients.Flows;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Tags;
using WildHealth.Common.Models.Patients;
using WildHealth.Common.Models.Tags;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.CommandHandlers.Patients;

public class GetPatientMetaDataCommandHandler : IRequestHandler<GetPatientMetaDataCommand, PatientMetaDataModel>
{
    private readonly IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> _cacheService;
    private readonly ITagRelationsService _tagRelationsService;
    private readonly IPatientsService _patientsService;
    private readonly IMapper _mapper;

    public GetPatientMetaDataCommandHandler(
        ITagRelationsService tagRelationsService, 
        IWildHealthSpecificCacheService<PatientMetaDataModel, PatientMetaDataModel> cacheService, 
        IPatientsService patientsService,
        IMapper mapper)
    {
        _tagRelationsService = tagRelationsService;
        _cacheService = cacheService;
        _patientsService = patientsService;
        _mapper = mapper;
    }

    public async Task<PatientMetaDataModel> Handle(GetPatientMetaDataCommand request, CancellationToken cancellationToken)
    {
        // Removed caching as there wasn't a good strategy here for invalidating cache and the cost to get this is very low
        // return await _cacheService.GetAsync(request.PatientId.GetHashCode().ToString(),
        //     async () => await GetMetaData(request.PatientId));
        
        return await GetMetaData(request.PatientId);
    }

    private async Task<PatientMetaDataModel> GetMetaData(int patientId)
    {
        var patient = await _patientsService.GetByIdAsync(patientId);
        var tagRelations = await _tagRelationsService.GetAllOfEntity(patient.UniversalId);
        var flow = new GetPatientMetaDataFlow(patient, tagRelations);
        var result = flow.Execute();
        return new PatientMetaDataModel
        {
            MetaTags = _mapper.Map<TagModel[]>(result.MetaTags),
            SubscriptionPlan = result.SubscriptionPlanDisplayName
        };
    }
}