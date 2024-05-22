using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class OfferPaymentPlanCommandHandler : IRequestHandler<OfferPaymentPlanCommand>
    {
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPatientsService _patientsService;
        private readonly IMediator _mediator;

        public OfferPaymentPlanCommandHandler(
            IPaymentPlansService paymentPlansService, 
            IPatientsService patientsService, 
            IMediator mediator)
        {
            _paymentPlansService = paymentPlansService;
            _patientsService = patientsService;
            _mediator = mediator;
        }

        public async Task Handle(OfferPaymentPlanCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.PatientId);
            var lastSubscription = patient.MostRecentSubscription;
            if (lastSubscription is not null && !lastSubscription.CanBeReplaced())
            {
                throw new AppException(HttpStatusCode.BadRequest, "Patient is not able to replace last subscription.");
            }
            
            var paymentPlan = await _paymentPlansService.GetActivePlanAsync(
                paymentPlanId: command.PaymentPlanId,
                paymentPeriodId: command.PaymentPeriodId,
                practiceId: command.PracticeId);

            var paymentPeriod = paymentPlan.PaymentPeriods.FirstOrDefault(x => x.Id == command.PaymentPeriodId);
            if (paymentPeriod is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(command.PaymentPeriodId), command.PaymentPeriodId);
                throw new AppException(HttpStatusCode.NotFound, "Payment period does not exist", exceptionParam);
            }

            var sendEmailCommand = new SendPaymentPlanOfferEmailCommand(
                patient: patient,
                paymentPlan: paymentPlan,
                paymentPeriod: paymentPeriod,
                practiceId: command.PracticeId);

            await _mediator.Send(sendEmailCommand, cancellationToken);
        }
    }
}