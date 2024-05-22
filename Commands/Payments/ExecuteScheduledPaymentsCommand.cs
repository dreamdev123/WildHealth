using MediatR;

namespace WildHealth.Application.Commands.Payments;

public record ExecuteScheduledPaymentsCommand : IRequest;