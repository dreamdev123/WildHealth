using MediatR;

namespace WildHealth.Application.Commands.PromoCodes;

public record DeletePromoCodeCommand(int Id) : IRequest;