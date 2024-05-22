using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.Domain.PaymentIssues;

public class ProcessSubscriptionPaymentIssuesCommandHandler : IRequestHandler<ProcessPaymentIssuesCommand>
{
    private readonly IPaymentIssuesService _paymentIssuesService;
    private readonly MaterializeFlow _materializer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPaymentService _paymentService;
    private readonly IPatientProfileService _patientProfileService;
    private readonly ILogger _logger;
    private readonly PaymentIssueOptions _config;
    private readonly IPatientsService _patientsService;

    public ProcessSubscriptionPaymentIssuesCommandHandler(
        IPaymentIssuesService paymentIssuesService, 
        IDateTimeProvider dateTimeProvider,
        ILogger<ProcessSubscriptionPaymentIssuesCommandHandler> logger, 
        MaterializeFlow materializer, 
        IPaymentService paymentService, 
        IPatientProfileService patientProfileService,
        IOptions<PaymentIssueOptions> options, 
        IPatientsService patientsService)
    {
        _paymentIssuesService = paymentIssuesService;
        _logger = logger;
        _materializer = materializer;
        _paymentService = paymentService;
        _patientProfileService = patientProfileService;
        _patientsService = patientsService;
        _dateTimeProvider = dateTimeProvider;
        _config = options.Value;
    }

    public async Task Handle(ProcessPaymentIssuesCommand command, CancellationToken cancellationToken)
    {
        var paymentIssues = await _paymentIssuesService.GetActiveAsync();
        
        foreach (var paymentIssue in paymentIssues)
        {
            var patient = await _patientsService.GetByIdAsync(paymentIssue.PatientId, PatientSpecifications.PatientWithIntegrations);
            var paymentLink = await _paymentService.CreateResolveCustomerPortalLinkAsync(patient).ToTry();
            var patientProfileLink = await _patientProfileService.GetProfileLink(paymentIssue.PatientId, paymentIssue.Patient.User.PracticeId);

            paymentLink.DoIfError(ex => _logger.LogError("Error during getting payment link. PaymentIssueId: {Id}. Error: {Error}", paymentIssue.GetId(), ex.Message));

            var result = await new ProcessPaymentIssueFlow(
                PaymentIssue: paymentIssue,
                NewStatus: null,
                NotificationTimeWindow: PaymentIssueNotificationTimeWindow.Default,
                Now: _dateTimeProvider.UtcNow(),
                Config: _config,
                PaymentLink: paymentLink.ValueOr(string.Empty),
                PatientProfileLink: patientProfileLink
            ).Materialize(_materializer).ToTry();
            
            result.DoIfError(ex => _logger.LogError("Error during processing subscription payment issue for with Id: {Id}. Error: {Error}", paymentIssue.GetId(), ex.Message));
        }
    }
}