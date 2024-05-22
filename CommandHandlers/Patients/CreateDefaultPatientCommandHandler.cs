using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.DnaFiles;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.PatientCreator;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients;

public class CreateDefaultPatientCommandHandler : IRequestHandler<CreateDefaultPatientCommand, Patient>
{
    private const string DefaultDnaFileName = "default_licensee_dna_report.txt";
    private const string DefaultUserNote = "paractice_test_patient";
    
    private readonly ILogger<CreateDefaultPatientCommandHandler> _logger;
    private readonly ITransactionManager _transactionManager;
    private readonly IPatientCreator _patientCreator;
    private readonly IPatientsService _patientsService;
    private readonly IDnaFilesService _dnaFilesService;
    private readonly IMediator _mediator;

    public CreateDefaultPatientCommandHandler(
        ILogger<CreateDefaultPatientCommandHandler> logger, 
        ITransactionManager transactionManager, 
        IPatientCreator patientCreator,
        IPatientsService patientsService, 
        IDnaFilesService dnaFilesService, 
        IMediator mediator)
    {
        _logger = logger;
        _transactionManager = transactionManager;
        _patientCreator = patientCreator;
        _patientsService = patientsService;
        _dnaFilesService = dnaFilesService;
        _mediator = mediator;
    }

    public async Task<Patient> Handle(CreateDefaultPatientCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Start create default patient for practice with Id [ID] {request.Practice.GetId()}");
        
        var location = request.Location;
        var practice = request.Practice;
        var employee = request.Employee;
        
        try
        {
            Patient? patient = null;
            
            await _transactionManager.Run(async () => {
                var createUserCommand = new CreateUserCommand(
                    firstName: "Test",
                    lastName: "Test",
                    email: $"patient_2{employee.User.Email}",
                    phoneNumber: "(111) 111-1111",
                    password: "Qwerty123",
                    birthDate: new DateTime(2000, 1, 1),
                    gender: Gender.Male,
                    userType: UserType.Patient,
                    practiceId: practice.GetId(),
                    billingAddress: NewAddress(location),
                    shippingAddress: NewAddress(location),
                    isVerified: true,
                    isRegistrationCompleted: true,
                    note: DefaultUserNote,
                    marketingSms: false
                );

                var user = await _mediator.Send(createUserCommand, cancellationToken);

                var patientOptions = new PatientOptions
                {
                    IsFellow = false
                };

                var newPatient = await _patientCreator.Create(user, patientOptions, location, request.DataTemplates);
                
                patient = await _patientsService.CreatePatientAsync(newPatient);
            });
            
            await SetDefaultDnaReportAsync(patient!, cancellationToken);

            return patient!;
        }
        catch (Exception e)
        {
            _logger.LogError($"Default patient for practice {practice.Name} was not created with [Error]: {e.ToString()}");
            return null!;
        }
    }

    private AddressModel NewAddress(Location location)
    {
        return new AddressModel
        {
            City = location.City,
            Country = location.Country,
            State = location.State,
            StreetAddress1 = "Test street",
            StreetAddress2 = "Test street2",
            ZipCode = location.ZipCode
        };
    }
    
    private async Task SetDefaultDnaReportAsync(Patient patient, CancellationToken cancellationToken)
    {
        var bytes = await _dnaFilesService.DownloadFileAsync(DefaultDnaFileName);
        
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, DefaultDnaFileName, DefaultDnaFileName);
        
        var command = new UploadInputsCommand(
            type: FileInputType.DnaReport,
            dataProvider: FileInputDataProvider.LabCorpElation,
            file: file,
            patientId: patient.GetId());

        await _mediator.Send(command, cancellationToken);
    }
}
