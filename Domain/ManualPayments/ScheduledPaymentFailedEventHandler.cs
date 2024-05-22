using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Domain.PaymentIssues;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.Domain.ManualPayments;

public class ScheduledPaymentFailedEventHandler : INotificationHandler<ScheduledPaymentFailedEvent>
{
    private readonly IIntegrationsService _integrationsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializer;
    private readonly IPatientsService _patientsService;
    private readonly IPaymentService _paymentService;
    private readonly IPatientProfileService _patientProfileService;
    private readonly PaymentIssueOptions _config;

    public ScheduledPaymentFailedEventHandler(
        IIntegrationsService integrationsService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializer, 
        IPatientsService patientsService, 
        IPaymentService paymentService, 
        IPatientProfileService patientProfileService,
        IOptions<PaymentIssueOptions> options)
    {
        _integrationsService = integrationsService;
        _dateTimeProvider = dateTimeProvider;
        _materializer = materializer;
        _patientsService = patientsService;
        _paymentService = paymentService;
        _patientProfileService = patientProfileService;
        _config = options.Value;
    }

    public async Task Handle(ScheduledPaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        var integration = await _integrationsService.GetAsync(notification.Vendor, notification.IntegrationInvoiceId);
        await new CreatePaymentIssueFlow(integration, _dateTimeProvider.UtcNow(), notification.PatientId).Materialize(_materializer);
        
        var paymentIssue = integration.PaymentIssues.LastActive()!; 
        var patientWithIntegration = await _patientsService.GetByIdAsync(paymentIssue.PatientId, PatientSpecifications.PatientWithIntegrations);
        var paymentLink = await _paymentService.CreateResolveCustomerPortalLinkAsync(patientWithIntegration).ToTry(); // Stripe throws for test accounts in dev/stage
        var patientProfileLink = await _patientProfileService.GetProfileLink(paymentIssue.PatientId, paymentIssue.Patient.User.PracticeId);

        await new ProcessPaymentIssueFlow(
            PaymentIssue: paymentIssue,
            NewStatus: PaymentIssueStatus.PatientNotified,
            NotificationTimeWindow: PaymentIssueNotificationTimeWindow.AllDay,
            Now: _dateTimeProvider.UtcNow(),
            Config: _config,
            PaymentLink: paymentLink.ValueOr(string.Empty),
            PatientProfileLink: patientProfileLink
        ).Materialize(_materializer);
    }
}