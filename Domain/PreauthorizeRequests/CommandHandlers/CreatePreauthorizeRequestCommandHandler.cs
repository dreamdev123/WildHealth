using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Application.Domain.PreauthorizeRequests.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Exceptions;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class CreatePreauthorizeRequestCommandHandler : IRequestHandler<CreatePreauthorizeRequestCommand, PreauthorizeRequest>
{
    private readonly IMediator _mediator;
    private readonly MaterializeFlow _materialize;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IEmployerProductService _employerProductService;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;

    public CreatePreauthorizeRequestCommandHandler(
        IMediator mediator, 
        MaterializeFlow materialize, 
        IPaymentPlansService paymentPlansService,
        IEmployerProductService employerProductService,
        IPreauthorizeRequestsService preauthorizeRequestsService)
    {
        _mediator = mediator;
        _materialize = materialize;
        _paymentPlansService = paymentPlansService;
        _employerProductService = employerProductService;
        _preauthorizeRequestsService = preauthorizeRequestsService;
    }

    public async Task<PreauthorizeRequest> Handle(CreatePreauthorizeRequestCommand command, CancellationToken cancellationToken)
    {
        await AssertNoRequests(command.Email);
        
        var createInitialUserCommand = new CreateInitialUserCommand(
            firstName: command.FirstName,
            lastName: command.LastName,
            email: command.Email,
            phoneNumber: string.Empty,
            birthday: DateTime.Now,
            userType: UserType.Unspecified,
            gender: Gender.None,
            practiceId: command.PracticeId
        );

        var user = await _mediator.Send(createInitialUserCommand, cancellationToken);

        var paymentPlans = await _paymentPlansService.GetByIdsAsync(new []{command.PaymentPlanId}, command.PracticeId);

        var paymentPlan = paymentPlans.FirstOrDefault();

        var paymentPeriod = paymentPlan?.PaymentPeriods.FirstOrDefault(x => x.Id == command.PaymentPeriodId);

        var paymentPrice = paymentPeriod?.Prices.FirstOrDefault(x => x.Id == command.PaymentPriceId);

        var employerProduct = command.EmployerProductId.HasValue
            ? await _employerProductService.GetByIdAsync(command.EmployerProductId.Value)
            : null;

        var flow = new CreatePreauthorizeRequestFlow(user, paymentPlan, paymentPeriod, paymentPrice, employerProduct);

        var result = await flow.Materialize(_materialize);

        return result.Select<PreauthorizeRequest>();
    }
    
    #region private

    private async Task AssertNoRequests(string email)
    {
        try
        {
            var existingRequest = await _preauthorizeRequestsService.GetByEmailAsync(email);

            if (existingRequest is not null)
            {
                throw new DomainException("Preauthorize request with this email already exists.");
            }
        }
        catch (EntityNotFoundException)
        {
            // ignore this exception as expected behavior
        }
    }
    
    #endregion
}