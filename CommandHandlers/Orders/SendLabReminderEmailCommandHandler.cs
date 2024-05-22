using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders;

public class SendLabReminderEmailCommandHandler : IRequestHandler<SendLabReminderEmailCommand, Unit>
{
    private const string Subject = "Lab Reminder";

    private static readonly string[] EmailContainerSettings =
    {
        SettingsNames.General.ApplicationBaseUrl,
        SettingsNames.Emails.HeaderUrl,
        SettingsNames.Emails.LogoUrl,
        SettingsNames.Emails.WhiteLogoUrl,
        SettingsNames.Emails.WHLinkLogoUrl,
        SettingsNames.Emails.InstagramUrl,
        SettingsNames.Emails.WHInstagramLogoUrl
    };

    private readonly ILabOrdersService _labOrdersService;
    private readonly IPracticeService _practiceService;
    private readonly ISettingsManager _settingsManager;
    private readonly IEmailFactory _emailFactory;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public SendLabReminderEmailCommandHandler(
        IPracticeService practiceService,
        ISettingsManager settingsManager,
        IEmailFactory emailFactory,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ILabOrdersService labOrdersService,
        ILogger<SendLabReminderEmailCommandHandler> logger)
    {
        _practiceService = practiceService;
        _settingsManager = settingsManager;
        _emailFactory = emailFactory;
        _emailService = emailService;
        _logger = logger;
        _labOrdersService = labOrdersService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Unit> Handle(SendLabReminderEmailCommand command, CancellationToken cancellationToken)
    {
        var order = await _labOrdersService.GetByIdAsync(command.Orderid);

        var patient = order.Patient;

        var practiceId = order.PracticeId;

        _logger.LogInformation($"Sending lab reminder email for order with [Id] = {order.GetId()} has been started.");

        var practice = await _practiceService.GetOriginalPractice(practiceId);

        var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

        if (!order.ExpectedCollectionDate.HasValue)
        {
            var exceptionParam = new AppException.ExceptionParameter(nameof(order.Id), order.GetId());
            throw new AppException(HttpStatusCode.BadRequest, "Order does not contain collection date.", exceptionParam);
        }

        var needToSend = order.ExpectedCollectionDate > DateTime.UtcNow.AddDays(7);
        if (!needToSend)
        {
            return Unit.Value;
        }

        var model = new EmailDataModel<LabReminderEmailModel>
        {
            Data = new LabReminderEmailModel
            {
                FirstName = order.Patient.User.FirstName,
                CollectionDate = (order.ExpectedCollectionDate ?? DateTime.UtcNow).Date,
                PracticeName = practice.Name,
                PracticeEmail = practice.Email,
                ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                PracticePhoneNumber = practice.PhoneNumber,
                PracticeAddress = $"{practice.Address.Address1} " +
                      $"{practice.Address.City} " +
                      $"{practice.Address.State} " +
                      $"{practice.Address.ZipCode}",
                PracticeId = practice.Id
            }
        };

        var email = await _emailFactory.Create(model);
        
        var sendAt = order.ExpectedCollectionDate!.Value.AddDays(-7);

        await _emailService.SendAsync(
            to: patient.User.Email,
            subject: Subject,
            body: email.Html,
            practiceId: patient.User.PracticeId,
            sendAt: sendAt > _dateTimeProvider.UtcNow() ? sendAt : null
        );

        _logger.LogInformation($"Sending lab reminder email for order with [Id] = {order.GetId()} has been finished.");

        return Unit.Value;
    }
}

