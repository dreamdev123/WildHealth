using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Common.Models.HealthSummaries;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class GetCurrentColonoscopyAndPsaForScreeningCommandHandler: IRequestHandler<GetCurrentColonoscopyAndPsaForScreeningCommand, (HealthSummaryValueModel,HealthSummaryValueModel)>
{
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IMapper _mapper;
    private readonly IQuestionnaireResultsService _questionnaireResultsService;

    public GetCurrentColonoscopyAndPsaForScreeningCommandHandler(
        IHealthSummaryService healthSummaryService, 
        IMapper mapper, 
        IQuestionnaireResultsService questionnaireResultsService)
    {
        _healthSummaryService = healthSummaryService;
        _mapper = mapper;
        _questionnaireResultsService = questionnaireResultsService;
    }
    
    public async Task<(HealthSummaryValueModel,HealthSummaryValueModel)> Handle(GetCurrentColonoscopyAndPsaForScreeningCommand request, CancellationToken cancellationToken)
    {
        var resultColonoscopy = await GetValueHealthSummary(request.PatientId, "DATE_OF_LAST_SCREENING").ToTry();
        
        var resultPsa = await GetValueHealthSummary(request.PatientId, "DATE_OF_PSA_OR_PROSTATE_EXAM").ToTry();
        
        if(!resultColonoscopy.IsSuccess() && !resultPsa.IsSuccess())
        {
            return await GetValueQuestionnaire(request.PatientId);
        }
        if (resultColonoscopy.IsSuccess() && !resultPsa.IsSuccess())
        {
            return (_mapper.Map<HealthSummaryValueModel>(resultColonoscopy.SuccessValue()), new HealthSummaryValueModel());
        }
        if (!resultColonoscopy.IsSuccess() && resultPsa.IsSuccess())
        {
            return (new HealthSummaryValueModel(), _mapper.Map<HealthSummaryValueModel>(resultPsa.SuccessValue()));
        }

        return (_mapper.Map<HealthSummaryValueModel>(resultColonoscopy.SuccessValue()), _mapper.Map<HealthSummaryValueModel>(resultPsa.SuccessValue()));
    }

    public async Task<HealthSummaryValue> GetValueHealthSummary(int patientId, string key)
    {
        var healthSummaryValue = await _healthSummaryService.GetByKeyAsync(patientId, key);

        if (string.IsNullOrEmpty(healthSummaryValue.Value))
        {
            throw new Exception();
        }

        return healthSummaryValue;
    }
    
    public async Task<(HealthSummaryValueModel,HealthSummaryValueModel)> GetValueQuestionnaire(int patientId)
    {
        var questionnaireResults = await _questionnaireResultsService.GetDetailedAsync(patientId).ToTry();

        var colonoscopyAnswer = questionnaireResults.IsSuccess()
            ? questionnaireResults.SuccessValue().Answers.FirstOrDefault(x => x.Key == "DATE_OF_LAST_SCREENING")
            : null;
        var psaAnswer = questionnaireResults.IsSuccess()
            ? questionnaireResults.SuccessValue().Answers.FirstOrDefault(x => x.Key == "DATE_OF_PSA_OR_PROSTATE_EXAM")
            : null;

        var resultColonoscopy = new HealthSummaryValueModel
        {
            Key = "DATE_OF_LAST_SCREENING",
            PatientId = patientId
        };

        var resultPsa = new HealthSummaryValueModel
        {
            Key = "DATE_OF_PSA_OR_PROSTATE_EXAM",
            PatientId = patientId
        };
            
        if (colonoscopyAnswer != null)
        {
            resultColonoscopy.Value = colonoscopyAnswer.Value;
        }
            
        if (psaAnswer != null)
        {
            resultPsa.Value = psaAnswer.Value;
        }
            
        return (
            resultColonoscopy,
            resultPsa
        );
    }
}