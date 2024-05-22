using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.PaymentIssues;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.PaymentIssues;

public interface IPaymentIssueManager
{
    Task CancelOrderPaymentIssueIfExists(int orderId);
    Task ResolveOrderPaymentIssueIfExists(int orderId);
    Task ThrowIfHasOutstandingPayment(int subscriptionId);
}

public class PaymentIssueManager : IPaymentIssueManager
{
    private readonly IPaymentIssuesService _paymentIssuesService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializer;
    private readonly PaymentIssueOptions _config;
    private readonly ILogger _logger;

    public PaymentIssueManager(
        IPaymentIssuesService paymentIssuesService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializer, 
        IOptions<PaymentIssueOptions> config, 
        ILogger<PaymentIssueManager> logger)
    {
        _paymentIssuesService = paymentIssuesService;
        _dateTimeProvider = dateTimeProvider;
        _materializer = materializer;
        _config = config.Value;
        _logger = logger;
    }
    
    public async Task CancelOrderPaymentIssueIfExists(int orderId)
    {
        await ChangeOrderPaymentIssueStatus(orderId, PaymentIssueStatus.UserCancelled);
    }
    
    public async Task ResolveOrderPaymentIssueIfExists(int orderId)
    {
        await ChangeOrderPaymentIssueStatus(orderId, PaymentIssueStatus.Resolved);
    }

    private async Task ChangeOrderPaymentIssueStatus(int orderId, PaymentIssueStatus newStatus)
    {
        var paymentIssue = await _paymentIssuesService.GetByOrderIdAsync(orderId).ToOption();

        if (!paymentIssue.HasValue()) return;

        var result = await new ProcessPaymentIssueFlow(
            PaymentIssue: paymentIssue.Value(),
            NewStatus: newStatus,
            NotificationTimeWindow: PaymentIssueNotificationTimeWindow.Default,
            Now: _dateTimeProvider.UtcNow(),
            Config: _config
        ).Materialize(_materializer).ToTry();

        result.DoIfError(ex =>
            _logger.LogError("Error during processing subscription payment issue for with Id: {Id}. Error: {Error}",
                paymentIssue.Value().GetId(), ex.Message));
    }

    public async Task ThrowIfHasOutstandingPayment(int subscriptionId)
    {
        var hasOutstandingPayment = await _paymentIssuesService.HasOutstandingPayment(subscriptionId);
        if (hasOutstandingPayment)
        {
            var errorMsg = $"Can't perform operation bacause subscription {subscriptionId} has outstanding payments";
            _logger.LogWarning(errorMsg);
            throw new DomainException(errorMsg);
        }
    }
}