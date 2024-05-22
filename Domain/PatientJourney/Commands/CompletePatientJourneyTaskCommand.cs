using MediatR;

namespace WildHealth.Application.Domain.PatientJourney.Commands;

public record CompletePatientJourneyTaskCommand(int PatientId, int JourneyTaskId, int PaymentPlanId, int PracticeId) : IRequest;