using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Vitals;
using WildHealth.Common.Models.HealthSummaries;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Constants;
using MediatR;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Models.Patient;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class GetVitalsSectionCommandHandler : IRequestHandler<GetVitalsSectionCommand, HealthSummaryValueModel[]>
{
    private const string VitalsKey = "VITAL_VALUE";
    
    private readonly IPatientsService _patientsService;
    private readonly IInputsService _inputsService;
    private readonly IVitalService _vitalService;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IAuthTicket _authTicket;

    public GetVitalsSectionCommandHandler(
        IPatientsService patientsService,
        IInputsService inputsService,
        IVitalService vitalService,
        IPermissionsGuard permissionsGuard,
        IAuthTicket authTicket)
    {
        _patientsService = patientsService;
        _inputsService = inputsService;
        _vitalService = vitalService;
        _permissionsGuard = permissionsGuard;
        _authTicket = authTicket;
    }

    public async Task<HealthSummaryValueModel[]> Handle(GetVitalsSectionCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(request.PatientId);
        if (_authTicket.GetUserType() == UserType.Employee)
        {
            _permissionsGuard.AssertPermissions(patient);
            // ok.
        }
        else if(_authTicket.GetUserType() == UserType.Patient && _authTicket.GetPatientId() == request.PatientId)
        {
            // ok.
        }
        else
        {
            // no.
            throw new AppException(HttpStatusCode.Unauthorized,
                $"You do not have access to patient {request.PatientId}");
        }

        var patientDomain = PatientDomain.Create(patient);
        var vitals = await _vitalService.GetLatestAsync(request.PatientId);

        var inputs = await _inputsService.GetGeneralInputsAsync(request.PatientId);
        
        HealthSummaryValueModel NewValue(string key, string? value) => new ()
        {
            PatientId = request.PatientId,
            Key = $"{VitalsKey}_{key}",
            Value = value
        };

        var values = new[]
        {
            NewValue("AGE", inputs.ChronologicalAge.ToString()),
            NewValue("SEX", patientDomain.GenderName),
            NewValue("HEIGHT", GetHeight(vitals)),
            NewValue("WEIGHT", GetWeight(vitals)),
            NewValue("BMI", GetBmi(vitals)),
            NewValue("BLOOD_PRESSURE", GetBloodPressure(vitals))
        };

        return values
            .Where(x=> !string.IsNullOrEmpty(x.Value))
            .ToArray();
    }

    private string? GetBloodPressure(IDictionary<string, VitalDetailsModel> vitals)
    {
        if (!(vitals.ContainsKey(VitalNames.SystolicBloodPressure.Name) 
                               && vitals.ContainsKey(VitalNames.DiastolicBloodPressure.Name)))
        {
            return null;
        }
        
        var systolic = vitals[VitalNames.SystolicBloodPressure.Name];
        var diastolic = vitals[VitalNames.DiastolicBloodPressure.Name];

        if (systolic.Value is null || diastolic.Value is null)
        {
            return null;
        }

        return $"{(int) systolic.Value}/{(int) diastolic.Value}";
    }

    private string? GetBmi(IDictionary<string, VitalDetailsModel> vitals)
    {
        if (!vitals.ContainsKey(VitalNames.BMI.Name))
        {
            return null;
        }

        var bmi = vitals[VitalNames.BMI.Name];

        return bmi.Value is null ? null : ((int)bmi.Value).ToString();
    }
    
    private string? GetWeight(IDictionary<string, VitalDetailsModel> vitals)
    {
        if (!vitals.ContainsKey(VitalNames.Weight.Name))
        {
            return null;
        }

        var weight = vitals[VitalNames.Weight.Name];

        return weight.Value is null ? null : $"{(int) weight.Value} {weight.Dimension}";
    }

    private string? GetHeight(IDictionary<string, VitalDetailsModel> vitals)
    {
        if (!vitals.ContainsKey(VitalNames.Height.Name))
        {
            return null;
        }

        var height = vitals[VitalNames.Height.Name];
        
        if (height?.Value is null)
        {
            return null;
        }

        var heightFt = height.Value / 12;
        var heightInc = height.Value % 12;

        var ft = heightFt > 0 ? $"{Convert.ToInt32(heightFt)} ft" : "";
        var inc = heightInc > 0 ? $"{Convert.ToInt32(heightInc)} in" : "";
		
        return $"{ft} {inc}";
    }
}