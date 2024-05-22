using MediatR;

namespace WildHealth.Application.Domain.PatientJourney.Commands;

public record DismissPatientJourneyTaskCommand(int PatientId, int JourneyTaskId) : IRequest;