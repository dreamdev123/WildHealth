using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.EventHandlers.PatientRecommendations;

public class UpdateKnowledgeBaseOnPatientRecommendationVerifiedEvent : INotificationHandler<PatientRecommendationVerifiedEvent>
{
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IGeneralRepository<Patient> _patientsRepository;

    public UpdateKnowledgeBaseOnPatientRecommendationVerifiedEvent(
        IPatientRecommendationsService patientRecommendationsService,
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient,
        IGeneralRepository<Patient> patientsRepository)
    {
        _patientRecommendationsService = patientRecommendationsService;
        _patientsRepository = patientsRepository;
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
    }

    public async Task Handle(PatientRecommendationVerifiedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var patientRecommendations in notification.PatientRecommendations
                     .Where(o => o.Verified)
                     .GroupBy(o => o.PatientId))
        {
            var patientRecommendationObjects =
                await _patientRecommendationsService.GetByIdsAsync(patientRecommendations.Select(o => o.GetId()).ToArray());
            
            var patient = await _patientsRepository
                .All()
                .Where(o => o.Id == patientRecommendations.Key)
                .Include(o => o.User)
                .FirstAsync(cancellationToken: cancellationToken);
            
            await _jennyKnowledgeBaseWebClient.StoreChunks(new DocumentChunkStoreRequestModel
            {
                UserUniversalId = patient.User.UserId(),
                Chunks = patientRecommendationObjects.Select(o => new DocumentChunkStoreModel()
                {
                    Document = o.Content,
                    ResourceType = AiConstants.ResourceTypes.PatientRecommendation,
                    ResourceId = o.UniversalId.ToString(),
                    Tags = o.Recommendation.Tags.Select(t => t.Tag.ToString()).ToArray(),
                    Personas = Array.Empty<string>()
                }).ToArray()
            });
        }
    }
}