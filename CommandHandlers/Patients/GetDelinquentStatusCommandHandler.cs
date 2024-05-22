using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Patients;

public class GetDelinquentStatusCommandHandler:IRequestHandler<GetDelinquentStatusCommand, DelinquentStatusModel[]>
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentIssuesService _paymentIssuesService;
    private readonly IPatientsService _patientsService;

    public GetDelinquentStatusCommandHandler(
        IPaymentService paymentService, 
        IPaymentIssuesService paymentIssuesService, 
        IPatientsService patientsService)
    {
        _paymentService = paymentService;
        _paymentIssuesService = paymentIssuesService;
        _patientsService = patientsService;
    }

    public async Task<DelinquentStatusModel[]> Handle(GetDelinquentStatusCommand request, CancellationToken cancellationToken)
    {
        var paymentIssues = await _paymentIssuesService.GetActiveAsync(request.PatientId);

        var paymentLink = await GetPaymentLink(paymentIssues);
        var result = paymentIssues
            .OrderBy(x => x.Type)
            .Select(x => new DelinquentStatusModel
            {
                Reason = GetReason(x),
                PaymentLinkText = GetPaymentLinkText(x),
                PaymentLink = paymentLink,
                AsOf = x.CreatedAt
            }).ToArray();
     
        return result;
    }

    private string GetPaymentLinkText(PaymentIssue paymentIssue) => paymentIssue.Type switch
    {
        PaymentIssueType.Subscription => "Update Now",
        _ => "Pay Invoice"
    };
    
    private async Task<string> GetPaymentLink(PaymentIssue[] paymentIssues)
    {
        if (!paymentIssues.Any()) return string.Empty;
        
        var patient = await _patientsService.GetByIdAsync(paymentIssues.First().PatientId, PatientSpecifications.PatientWithIntegrations);
        var linkTry = await _paymentService.CreateResolveCustomerPortalLinkAsync(patient).ToTry();
        return linkTry.ValueOr(string.Empty);
    }

    private static string GetReason(PaymentIssue paymentIssue) => paymentIssue.Type switch
    {
        PaymentIssueType.Subscription => "Oops! Your recent payment couldn't be processed. To continue enjoying Wild Health services, please update your payment info ASAP.",
        _ => "Oops! Your recent payment couldn't be processed. Please update your payment info and complete the outstanding payment."
    };
}