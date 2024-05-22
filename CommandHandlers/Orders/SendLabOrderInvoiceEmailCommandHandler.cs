using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Infrastructure.EmailFactory;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Practices;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class SendLabOrderInvoiceEmailCommandHandler : IRequestHandler<SendLabOrderInvoiceEmailCommand>
    {
        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl
        };
        
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly ILogger _logger;

        public SendLabOrderInvoiceEmailCommandHandler(
            IPracticeService practiceService, 
            ISettingsManager settingsManager,
            IEmailFactory emailFactory, 
            IEmailIntegrationService emailIntegrationService, 
            ILogger<SendLabOrderInvoiceEmailCommandHandler> logger)
        {
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _logger = logger;
        }

        public async Task Handle(SendLabOrderInvoiceEmailCommand command, CancellationToken cancellationToken)
        {          
            var order = command.Order;
            
            _logger.LogInformation($"Sending lab order invoice email for order with [Id] = {order.GetId()} has been started.");

            var patient = order.Patient;

            var practiceId = patient.User.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(order.PracticeId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var items = order.Items.Select(x =>
            {
                var addOn = x.AddOn;

                return new InvoiceItemEmailModel
                {
                    Quantity = 1,
                    Name = addOn.Name,
                    Price = addOn.GetPrice()
                };
            }).ToArray();

            var totalPrice = items.Select(x => x.Price).Sum();
            
            var email = await _emailFactory.Create(new EmailDataModel<LabOrderInvoiceEmailModel>
            {
                Data = new LabOrderInvoiceEmailModel
                {
                    Summary = new InvoiceSummaryEmailModel
                    {
                        TotalPrice = totalPrice,
                        SubTotalPrice = totalPrice,
                        TotalTax = 0
                    },
                    FirstName = patient.User.FirstName,
                    OrderNumber = order.Number,
                    Date = order.CreatedAt,
                    Items = items.ToArray(),
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                    HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                    LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                    PracticePhoneNumber = practice.PhoneNumber,
                    PracticeAddress = $"{practice.Address.Address1} " +
                        $"{practice.Address.City} " +
                        $"{practice.Address.State} " +
                        $"{practice.Address.ZipCode}",
                    PracticeId = practiceId
                }
            });
        
            var subject = $"{practice.Name} Lab Invoice";

            await _emailIntegrationService.SendEmailAndEventAsync(
                patient.User.Email, 
                subject, 
                email.Html, 
                patient.User.PracticeId,
                nameof(LabOrderInvoiceEmailModel),
                patient.User
            );

            _logger.LogInformation($"Sending lab order invoice email for order with [Id] = {order.GetId()} has been finished.");
        }
    }
}