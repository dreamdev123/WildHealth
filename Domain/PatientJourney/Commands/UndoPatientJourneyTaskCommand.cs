using MediatR;

namespace WildHealth.Application.Domain.PatientJourney.Commands;

public record UndoPatientJourneyTaskCommand(int PatientId, int JourneyTaskId, int PaymentPlanId, int PracticeId) : IRequest;