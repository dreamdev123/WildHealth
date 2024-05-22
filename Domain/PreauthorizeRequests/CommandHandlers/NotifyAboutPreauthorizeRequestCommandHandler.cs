using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Application.Domain.PreauthorizeRequests.Flows;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.DateTimes;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Settings;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class NotifyAboutPreauthorizeRequestCommandHandler : IRequestHandler<NotifyAboutPreauthorizeRequestCommand>
{
    private readonly AppOptions _appOptions;
    private readonly MaterializeFlow _materialize;
    private readonly ISettingsManager _settingsManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmployerProductService _employerProductService;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;

    public NotifyAboutPreauthorizeRequestCommandHandler(
        IOptions<AppOptions> appOptions,
        MaterializeFlow materialize, 
        ISettingsManager settingsManager,
        IDateTimeProvider dateTimeProvider,
        IEmployerProductService employerProductService,
        IPreauthorizeRequestsService preauthorizeRequestsService)
    {
        _appOptions = appOptions.Value;
        _materialize = materialize;
        _settingsManager = settingsManager;
        _dateTimeProvider = dateTimeProvider;
        _employerProductService = employerProductService;
        _preauthorizeRequestsService = preauthorizeRequestsService;
    }

    public async Task Handle(NotifyAboutPreauthorizeRequestCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();

        var requests = await _preauthorizeRequestsService.GetByIdsAsync(command.Ids);

        var employerProductIds = requests
            .Where(x => x.EmployerProductId.HasValue)
            .Select(x => x.EmployerProductId!.Value)
            .ToArray();

        var employerProducts = await _employerProductService.GetEmployerProductsByIdsAsync(employerProductIds);
        
        var appUrl = await _settingsManager.GetSetting<string>(SettingsNames.General.ApplicationBaseUrl, (int)PlanPlatform.WildHealth);

        var signUpUrl = _appOptions.PreauthorizeSignUpUrl;
        
        var flow = new NotifyAboutPreauthorizeRequestFlow(requests, employerProducts, utcNow, appUrl, signUpUrl);

        await flow.Materialize(_materialize);
    }
}