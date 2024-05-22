using MediatR;

namespace WildHealth.Application.Commands.PromoCodes;

public record DeactivatePromoCodeCommand(int Id) : IRequest;