using System;
using MediatR;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.Events.PatientRecommendations;

public record PatientRecommendationRemovedEvent(
    string UserUniversalId,
    Guid PatientRecommendationUniversalId) : INotification
{
    
}