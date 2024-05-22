using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.ManualPayments;

public class ExecuteScheduledPaymentsCommandHandler : IRequestHandler<ExecuteScheduledPaymentsCommand>
{
    private readonly IPatientsService _patientsService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializer;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IGeneralRepository<PaymentScheduleItem> _paymentScheduleItemRepository;
    private readonly ILogger<ExecuteScheduledPaymentsCommandHandler> _logger;

    public ExecuteScheduledPaymentsCommandHandler(
        IDateTimeProvider dateTimeProvider, 
        IPatientsService patientsService, 
        ISubscriptionService subscriptionService, 
        MaterializeFlow materializer, 
        IIntegrationServiceFactory integrationServiceFactory, 
        IGeneralRepository<PaymentScheduleItem> paymentScheduleItemRepository, 
        ILogger<ExecuteScheduledPaymentsCommandHandler> logger)
    {
        _dateTimeProvider = dateTimeProvider;
        _patientsService = patientsService;
        _subscriptionService = subscriptionService;
        _materializer = materializer;
        _integrationServiceFactory = integrationServiceFactory;
        _paymentScheduleItemRepository = paymentScheduleItemRepository;
        _logger = logger;
    }

    public async Task Handle(ExecuteScheduledPaymentsCommand request, CancellationToken cancellationToken)
    {
        var today = _dateTimeProvider.UtcNow().Date;
        var schedules = await _paymentScheduleItemRepository.All()
            .Where(x => x.DueDate.Date == today && string.IsNullOrEmpty(x.InvoiceStatus))
            .Include(x => x.Payment)
            .ThenInclude(x => x.Subscriptions)
            .ToListAsync(cancellationToken);

        foreach (var schedule in schedules)
        {
            var paymentResult = await ExecutePayment(schedule).ToTry();
            paymentResult.DoIfError(ex => _logger.LogError("Couldn't process payment schedule {PaymentScheduleItem}. Error: {Error}", schedule, ex));
        }
    }

    private async Task<PaymentScheduleItem> ExecutePayment(PaymentScheduleItem schedule)
    {
        var subscription = await _subscriptionService.GetAsync(schedule.Payment.Subscriptions.First().GetId());
        if (!subscription.IsActive) return schedule;

        var patient = await _patientsService.GetByIdAsync(subscription.PatientId,PatientSpecifications.PatientWithSubscriptionAndIntegrations);
        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
        
        var invoice = await integrationService.CreatePremiumSubscriptionPaymentAsync(patient, schedule.Amount, true);
        await new UpdatePaymentScheduleItemFlow(schedule, invoice, subscription.PatientId).Materialize(_materializer);
        
        return schedule;
    }
}