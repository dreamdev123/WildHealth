using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Application.Utils.LabInputsInitializer;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Settings;
using WildHealth.Common.Constants;

namespace WildHealth.Application.Utils.PatientCreator;

/// <summary>
/// <see cref="IPatientCreator"/>
/// </summary>
public class PatientCreator : IPatientCreator
{
    private static readonly string[] LabVendorSettings =
    {
        SettingsNames.Labs.DefaultVendorName
    };

    private readonly ILabInputsInitializer _labInputsInitializer;
    private readonly ILabVendorsService _labVendorsService;
    private readonly ISettingsManager _settingsManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<IPatientCreator> _logger;

    public PatientCreator(
        ILabInputsInitializer labInputsInitializer,
        ILabVendorsService labVendorsService,
        ISettingsManager settingsManager,
        IDateTimeProvider dateTimeProvider,
        ILogger<IPatientCreator> logger
        )
    {
        _labInputsInitializer = labInputsInitializer;
        _labVendorsService = labVendorsService;
        _settingsManager = settingsManager;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Patient> Create(User user, PatientOptions? patientOptions, Location location, IDictionary<string, string>? dataTemplates)
    {
        _logger.LogInformation($"Creating Patient for user [id] : {user.Id} and [locationId]: {location.Id} ");
        var patient = new Patient(user, patientOptions, location);

        var settings = dataTemplates is null || !dataTemplates.Any()
            ? await _settingsManager.GetSettings(LabVendorSettings, location.PracticeId)
            : dataTemplates;

        var labVendorName = settings[SettingsNames.Labs.DefaultVendorName];

        var labVendor = await _labVendorsService.GetByName(labVendorName);

        patient.SetInputsAggregator(await _labInputsInitializer.Initialize(patient.InputsAggregator, labVendor, user.Gender, user.Birthday ?? DateTime.UtcNow, true));
        
        //The caller can adjust this date if necessary, but we'll provide a sensible default here.
        patient.SetRegistrationDate(_dateTimeProvider.UtcNow());
        _logger.LogInformation($"Finished Patient created for user [id] : {user.Id} and [locationId]: {location.Id} ");
        return patient;
    }
    
    public async Task<Patient> AddPatientInputsAggregator(User user, Patient patient, Location location,
        IDictionary<string, string>? dataTemplates)
    {
     
        _logger.LogInformation($"Adding Inputs aggregator for Patient [id] : {patient.Id} ");
        
        var settings = dataTemplates is null || !dataTemplates.Any()
            ? await _settingsManager.GetSettings(LabVendorSettings, location.PracticeId)
            : dataTemplates;

        var labVendorName = settings[SettingsNames.Labs.DefaultVendorName];

        var labVendor = await _labVendorsService.GetByName(labVendorName);

        patient.SetInputsAggregator(await _labInputsInitializer.Initialize(patient.InputsAggregator, labVendor, user.Gender, user.Birthday ?? DateTime.UtcNow, true));
        
        //The caller can adjust this date if necessary, but we'll provide a sensible default here.
        patient.SetRegistrationDate(_dateTimeProvider.UtcNow());
        
        _logger.LogInformation($"Finished Adding Inputs aggregator for Patient [id] : {patient.Id} ");
        return patient;
    }
    
    public async Task<Patient> CatchUpPatientInputsAggregator(User user, Patient patient, Location location,
        IDictionary<string, string>? dataTemplates)
    {
        _logger.LogInformation($"Catching up Inputs aggregator for Patient [id] : {patient.Id} ");
        
        var settings = dataTemplates is null || !dataTemplates.Any()
            ? await _settingsManager.GetSettings(LabVendorSettings, location.PracticeId)
            : dataTemplates;

        var labVendorName = settings[SettingsNames.Labs.DefaultVendorName];

        var labVendor = await _labVendorsService.GetByName(labVendorName);

        patient.SetInputsAggregator(await _labInputsInitializer.Initialize(patient.InputsAggregator, labVendor, user.Gender, user.Birthday ?? DateTime.UtcNow, true));
        
        _logger.LogInformation($"Finished catching up Inputs aggregator for Patient [id] : {patient.Id} ");
        return patient;
    }
}