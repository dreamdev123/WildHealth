using MediatR;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.Events.PatientRecommendations;

public record PatientRecommendationVerifiedEvent(PatientRecommendation[] PatientRecommendations) : INotification
{
    
}