using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Domain.Entities.Users;
using WildHealth.Application.Domain.PreauthorizeRequests.Flows;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.PaymentPlans;
using MediatR;
using WildHealth.Application.Commands.Users;
using WildHealth.Common.Models.Users;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class UpdatePreauthorizeRequestCommandHandler : IRequestHandler<UpdatePreauthorizeRequestCommand, PreauthorizeRequest>
{
    private readonly MaterializeFlow _materialize;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IEmployerProductService _employerProductService;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;
    private readonly IMediator _mediator;

    public UpdatePreauthorizeRequestCommandHandler(
        MaterializeFlow materialize, 
        IPaymentPlansService paymentPlansService, 
        IEmployerProductService employerProductService, 
        IPreauthorizeRequestsService preauthorizeRequestsService,
        IMediator mediator)
    {
        _materialize = materialize;
        _paymentPlansService = paymentPlansService;
        _employerProductService = employerProductService;
        _preauthorizeRequestsService = preauthorizeRequestsService;
        _mediator = mediator;
    }

    public async Task<PreauthorizeRequest> Handle(UpdatePreauthorizeRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _preauthorizeRequestsService.GetByIdAsync(command.Id);
        
        var paymentPlans = await _paymentPlansService.GetByIdsAsync(new []{command.PaymentPlanId}, request.User.PracticeId);

        var paymentPlan = paymentPlans.FirstOrDefault();

        var paymentPeriod = paymentPlan?.PaymentPeriods.FirstOrDefault(x => x.Id == command.PaymentPeriodId);

        var paymentPrice = paymentPeriod?.Prices.FirstOrDefault(x => x.Id == command.PaymentPriceId);

        var employerProduct = command.EmployerProductId.HasValue
            ? await _employerProductService.GetByIdAsync(command.EmployerProductId.Value)
            : null;

        var flow = new UpdatePreauthorizeRequestFlow(request, paymentPlan, paymentPeriod, paymentPrice, employerProduct);

        await UpdateUserAsync(request, command);
        
        var result = await flow.Materialize(_materialize);

        return result.Select<PreauthorizeRequest>();
    }
    
    #region private

    private async Task UpdateUserAsync(PreauthorizeRequest request, UpdatePreauthorizeRequestCommand command)
    {
        await _mediator.Send(new UpdateUserCommand(
            id: request.UserId,
            firstName: command.FirstName,
            lastName: command.LastName,
            email: command.Email,
            birthday: request.User.Birthday,
            gender: request.User.Gender,
            phoneNumber: request.User.PhoneNumber,
            billingAddress: new AddressModel
            {
                City = request.User.BillingAddress.City,
                Country = request.User.BillingAddress.Country,
                State = request.User.BillingAddress.State,
                ZipCode = request.User.BillingAddress.ZipCode,
                StreetAddress1 = request.User.BillingAddress.StreetAddress1,
                StreetAddress2 = request.User.BillingAddress.StreetAddress2,
            },
            shippingAddress: new AddressModel
            {
                City = request.User.ShippingAddress.City,
                Country = request.User.ShippingAddress.Country,
                State = request.User.ShippingAddress.State,
                ZipCode = request.User.ShippingAddress.ZipCode,
                StreetAddress1 = request.User.ShippingAddress.StreetAddress1,
                StreetAddress2 = request.User.ShippingAddress.StreetAddress2,
            },
            userType: UserType.Unspecified
        ));
    }

    #endregion
}