using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.HealthScore;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.HealthScore;
using WildHealth.Domain.Models.Patient;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Inputs;

public class SetPatientMesaValueOnInputCommandHandler: IRequestHandler<SetPatientMesaValueOnInputCommand>
{
    private readonly ILogger<SetPatientMesaValueOnInputCommandHandler> _logger;
    private readonly IPatientsService _patientsService;
    private readonly IInputsService _inputsService;
    private readonly IMapper _mapper;
    private readonly IHealthScoreService _healthScoreService;
    
    
    public SetPatientMesaValueOnInputCommandHandler(
        IPatientsService patientsService,
        IInputsService inputsService,
        IHealthScoreService healthScoreService,
        IMapper mapper,
        ILogger<SetPatientMesaValueOnInputCommandHandler> logger
        )
    {
        _patientsService = patientsService;
        _healthScoreService = healthScoreService;
        _inputsService = inputsService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Handle(SetPatientMesaValueOnInputCommand command, CancellationToken cancellationToken)
    {
        
        //Grab domain data
        var specification = PatientSpecifications.PatientWithAggregationInputs;
        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);
        var patientDomain = PatientDomain.Create(patient);
        
        var patientScoreResponseModel = await _healthScoreService.GetPatientHealthScore(command.PatientId.ToString());
        
        if (patientScoreResponseModel.Terms?.Count() > 0)
        {
            var patientScore = _mapper.Map<HealthScoreModel>(patientScoreResponseModel);
            var generalInputs = await _inputsService.GetGeneralInputsAsync(patient.GetId());
            generalInputs = patientDomain.SetCalculatedMesaValue(patientScore, generalInputs);
            await _inputsService.UpdateGeneralInputsAsync(generalInputs, patient.GetId());
        }
    }
}