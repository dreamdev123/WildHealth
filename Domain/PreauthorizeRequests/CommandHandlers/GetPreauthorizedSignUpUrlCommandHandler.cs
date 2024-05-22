using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Application.Domain.PreauthorizeRequests.Extensions;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class GetPreauthorizedSignUpUrlCommandHandler : IRequestHandler<GetPreauthorizedSignUpUrlCommand, string>
{
    private readonly AppOptions _appOptions;
    private readonly ISettingsManager _settingsManager;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;

    public GetPreauthorizedSignUpUrlCommandHandler(
        IOptions<AppOptions> appOptions,
        ISettingsManager settingsManager,
        IPreauthorizeRequestsService preauthorizeRequestsService)
    {
        _appOptions = appOptions.Value;
        _settingsManager = settingsManager;
        _preauthorizeRequestsService = preauthorizeRequestsService;
    }
    
    public async Task<string> Handle(GetPreauthorizedSignUpUrlCommand command, CancellationToken cancellationToken)
    {
        var request = await _preauthorizeRequestsService.GetByIdAsync(command.PreauthorizeRequestId);
        
        var appUrl = await _settingsManager.GetSetting<string>(SettingsNames.General.ApplicationBaseUrl, (int)PlanPlatform.WildHealth);

        var signUpUrl = _appOptions.PreauthorizeSignUpUrl;
        
        return request.GeneratePersonalRegistrationLink(signUpUrl, appUrl);
    }
}