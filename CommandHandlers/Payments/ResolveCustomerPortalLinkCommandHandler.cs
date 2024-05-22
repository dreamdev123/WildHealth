using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.User;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Payments;

public class ResolveCustomerPortalLinkCommandHandler : IRequestHandler<ResolveCustomerPortalLinkCommand, string>
{
    private readonly IConfirmCodeService _confirmCodeService;
    private readonly IPatientsService _patientsService;
    private readonly IPaymentService _paymentService;

    public ResolveCustomerPortalLinkCommandHandler(
        IConfirmCodeService confirmCodeService, 
        IPatientsService patientsService, 
        IPaymentService paymentService)
    {
        _confirmCodeService = confirmCodeService;
        _patientsService = patientsService;
        _paymentService = paymentService;
    }

    public async Task<string> Handle(ResolveCustomerPortalLinkCommand command, CancellationToken cancellationToken)
    {
        var code = await _confirmCodeService.ConfirmAsync(command.Code, ConfirmCodeType.CheckoutSession);

        var patient = await _patientsService.GetByUserIdAsync(code.UserId);

        var link = await _paymentService.CreateCustomerPortalLinkAsync(patient);

        return link;
    }
}