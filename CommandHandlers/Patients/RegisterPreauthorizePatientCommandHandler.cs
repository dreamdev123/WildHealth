using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Patients.Flows;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Domain.PreauthorizeRequests.Services;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Domain.Models.Patient;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Patients;
using WildHealth.IntegrationEvents.Patients;
using WildHealth.IntegrationEvents.Patients.Payloads;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using Newtonsoft.Json;
using AutoMapper;
using MediatR;
using WildHealth.Common.Models.Agreements;

namespace WildHealth.Application.CommandHandlers.Patients;

public class RegisterPreauthorizePatientCommandHandler : IRequestHandler<RegisterPreauthorizePatientCommand, CreatedPatientModel>
{
    private readonly IMediator _mediator;
    private readonly IPreauthorizeRequestsService _preauthorizeRequestsService;
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IEmployerProductService _employerProductService;
    private readonly IPatientsService _patientsService;
    private readonly ILocationsService _locationsService;
    private readonly ITransactionManager _transactionManager;
    private readonly IUsersService _usersService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly IDurableMediator _durableMediator;
    private readonly MaterializeFlow _materializeFlow;
    
    public RegisterPreauthorizePatientCommandHandler(
        IMediator mediator,
        IPreauthorizeRequestsService preauthorizeRequestsService,
        IIntegrationServiceFactory integrationServiceFactory,
        IEmployerProductService employerProductService,
        IPatientsService patientsService,
        ILocationsService locationsService,
        ITransactionManager transactionManager,
        IUsersService usersService,
        IDateTimeProvider dateTimeProvider,
        IFeatureFlagsService featureFlagsService,
        IMapper mapper,
        ILogger<RegisterPatientCommandHandler> logger,
        IEventBus eventBus, 
        IDurableMediator durableMediator, 
        MaterializeFlow materializeFlow)
    {
        _mediator = mediator;
        _preauthorizeRequestsService = preauthorizeRequestsService;
        _integrationServiceFactory = integrationServiceFactory;
        _employerProductService = employerProductService;
        _patientsService = patientsService;
        _locationsService = locationsService;
        _transactionManager = transactionManager;
        _usersService = usersService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
        _featureFlagsService = featureFlagsService;
        _logger = logger;
        _eventBus = eventBus;
        _durableMediator = durableMediator;
        _materializeFlow = materializeFlow;
    }
    
    public async Task<CreatedPatientModel> Handle(RegisterPreauthorizePatientCommand command, CancellationToken cancellationToken)
    {
        var preauthorizeRequest = await _preauthorizeRequestsService.GetByTokenAsync(command.PreauthorizeRequestToken);
        
        var employerProduct = await GetEmployerProductAsync(preauthorizeRequest);
        
        // All patients are going to the MainOffice Pod regardless of retail or fellowship
        // https://wildhealth.atlassian.net/browse/CLAR-2483
        var location = await _locationsService.GetDefaultLocationAsync(command.PracticeId);
        
        var integrationService = await _integrationServiceFactory.CreateAsync(command.PracticeId);
        
        await using var transaction = _transactionManager.BeginTransaction();

        var patientCreatedModels = Array.Empty<PatientCreatedModel>();

        var cardIntegrationId = string.Empty;

        var user = await _usersService.GetByEmailAsync(command.Email);

        try
        {
            var patient = await RegisterPatientAsync(command, location, cancellationToken);

            patientCreatedModels = (await _mediator.Send(new CreatePaymentIntegrationAccountCommand(
                patientId: patient.GetId()
            ), cancellationToken)).ToArray();

            var card = await integrationService.CopyCardAsync(patient, employerProduct);

            cardIntegrationId = card.Id;
            
            // Creates subscription in DB
            // Creates subscription in integration service
            // Creates PurchasePayroll entries
            var buySubscriptionCommand = new BuyNewSubscriptionCommand(
                patient: patient,
                employerProduct: employerProduct,
                paymentPriceId: preauthorizeRequest.PaymentPriceId,
                paymentPeriodId: preauthorizeRequest.PaymentPeriodId,
                agreements: command.Agreements,
                promoCode: string.Empty
            );

            var subscription = await _mediator.Send(buySubscriptionCommand, cancellationToken);

            _logger.LogInformation($"Patient with [Id] = {patient.Id} registered successfully.");

            var postPatientRegistrationProcessesEvent = new PatientRegisteredEvent(
                PracticeId: command.PracticeId,
                PatientId: patient.GetId(),
                UniversalUserId: patient.User.UniversalId.ToString(),
                EmployeeId: null,
                LinkedEmployeeId: null,
                LocationId: location.GetId(),
                PaymentPriceId: preauthorizeRequest.PaymentPriceId,
                SubscriptionId: subscription.GetId(),
                FounderId: null,
                InviteCode: string.Empty,
                IsTrialPlan: false,
                EmployerProductKey: employerProduct?.Key,
                LeadSource: null,
                AddonIds: command.AddOnIds
            );
            
            await transaction.CommitAsync(cancellationToken);
            
            await _durableMediator.Publish(postPatientRegistrationProcessesEvent);

            var authResult = await _mediator.Send(new AuthenticateAfterCheckoutCommand(
                email: patient.User.Email,
                practiceId: patient.User.PracticeId
            ));

            var patientModel = _mapper.Map<CreatedPatientModel>(patient);

            patientModel.Authentication = authResult;
            
            var patientDomain = PatientDomain.Create(patient);
            patientModel = patientModel.EnrichPatientModel(patientDomain, patientModel, subscription);

            return patientModel;
        }
        catch (Exception ex)
        {
            var failedRegistrationData = JsonConvert.SerializeObject(command, Formatting.Indented);
            
            _logger.LogError($"Registration of patient with [Email] = {command.Email} [Phone] = {command.PhoneNumber} was failed. [Details] = \n{failedRegistrationData}\n {ex}");

            if (user is not null)
            {
                await _eventBus.Publish(new PatientIntegrationEvent(
                        payload: new PatientRegisterFailedPayload(
                            email: command.Email,
                            phone: command.PhoneNumber,
                            universalId: user.UniversalId.ToString(),
                            reason: ex.Message
                        ),
                        eventDate: DateTime.UtcNow), cancellationToken: cancellationToken
                );
            }
            
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch(Exception e)
            {
                _logger.LogError($"Unable to rollback transaction at failed registration of patient with [Email] = {command.Email}. {e.Message}");
            }

            if (patientCreatedModels.Any(o => o.IntegrationVendorPurpose == IntegrationVendorPurpose.Payment))
            {
                var patientIntegrationId = patientCreatedModels
                    .First(o => o.IntegrationVendorPurpose == IntegrationVendorPurpose.Payment).IntegrationId;

                if (!string.IsNullOrEmpty(cardIntegrationId))
                {
                    await integrationService.TryDeleteCardAsync(
                        cardId: cardIntegrationId,
                        patientId: patientIntegrationId
                    );
                }
                
                await integrationService.TryDeletePatientAsync(patientCreatedModels);
            }

            throw;
        }
    }

    #region private

    private async Task<EmployerProduct> GetEmployerProductAsync(PreauthorizeRequest request)
    {
        if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
        {
            return await _employerProductService.GetByKeyAsync();
        }

        if (!request.EmployerProductId.HasValue)
        {
            return await _employerProductService.GetByKeyAsync();
        }
        
        return await _employerProductService.GetByIdAsync(request.EmployerProductId.Value);
    }
    
    private async Task<Patient> RegisterPatientAsync(RegisterPreauthorizePatientCommand command, Location location, CancellationToken cancellationToken)
    {
        var user = await _usersService.GetByEmailAsync(command.Email);
        
        var isUserExist = !(user is null);

        var isPatientExist = isUserExist && await IsPatientExistAsync(user!);

        var patientOptions = new PatientOptions
        {
            IsFellow = false,
            IsCrossFitAssociated = false
        };

        var patient = isPatientExist
            ? await UpdateExistingPatientAsync(command, user!, patientOptions, cancellationToken)
            : await CreateNewPatientAsync(user!, command, patientOptions, location, cancellationToken);
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Case where patient never had a subscription
        // Action: change the practiceId on their User/UserIdentity records
        var isDifferentPractice = command.PracticeId != patient.User.PracticeId;
        if (isDifferentPractice)
        {
            var changePracticeCommand = new ChangePatientsPracticeCommand(
                patientId: patient.GetId(),
                newPracticeId: command.PracticeId);

            patient = await _mediator.Send(changePracticeCommand, cancellationToken);
        }

        return patient;
    }

    private async Task<bool> IsPatientExistAsync(User user)
    {
        if (user.Identity.Type == UserType.Employee)
        {
            throw new AppException(HttpStatusCode.BadRequest, "User already registered");
        }
            
        try
        {
            var patient = await _patientsService.GetByUserIdAsync(user.GetId());

            return !(patient is null);
        }
        catch (AppException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<Patient> UpdateExistingPatientAsync(
        RegisterPreauthorizePatientCommand command,
        User user,
        PatientOptions options,
        CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByUserIdAsync(user.GetId());

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Case where patient has existing subscription
        // Action: Prevent patient from registering.  They should cancel their prior membership first
        var isPatientActive = patient.CurrentSubscription is not null;
        if (isPatientActive)
        {
            throw new AppException(HttpStatusCode.BadRequest, $"{user.Email} already has an active membership.");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Case where patient had a prior subscription that's now cancelled AND practiceId is different
        // Action: Clone their prior patient account to a new patient account and configure for new practice
        var isPatientHasAnySubscriptions = patient.Subscriptions.Any();
        var mostRecentSubscription = patient.MostRecentSubscription;
        if (isPatientHasAnySubscriptions && mostRecentSubscription.PracticeId != command.PracticeId)
        {
            var changePracticeCommand = new ChangePatientsPracticeCommand(
                patientId: patient.GetId(),
                newPracticeId: command.PracticeId);

            patient = await _mediator.Send(changePracticeCommand, cancellationToken);
        }

        var updateUserCommand = new UpdateUserCommand(
            id: patient.UserId,
            firstName: command.FirstName,
            lastName: command.LastName,
            birthday:command.Birthday,
            gender: command.Gender,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            billingAddress: command.BillingAddress,
            shippingAddress: command.ShippingAddress,
            userType: UserType.Patient,
            isRegistrationCompleted: true
        );

        await _mediator.Send(updateUserCommand, cancellationToken);
        
        var resetPasswordCommand = new ResetPatientPasswordCommand(patient.GetId(), command.Password, false);

        await _mediator.Send(resetPasswordCommand, cancellationToken);

        if (!patient.RegistrationDate.HasValue)
        {
            patient.SetRegistrationDate(_dateTimeProvider.Now());
            patient.Options = options;
            await _patientsService.UpdateAsync(patient);
        }
       
        return await _patientsService.GetByIdAsync(patient.GetId());
    }

    private async Task<Patient> CreateNewPatientAsync(
        User user,
        RegisterPreauthorizePatientCommand command, 
        PatientOptions patientOptions,
        Location location,
        CancellationToken cancellationToken)
    {
        if (user is null)
        {
            var createUserCommand = new CreateUserCommand(
                firstName: command.FirstName,
                lastName: command.LastName,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                password: command.Password,
                birthDate: command.Birthday,
                gender: command.Gender,
                userType: UserType.Patient,
                practiceId: command.PracticeId,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress,
                isVerified: true,
                isRegistrationCompleted: true
            );

            user = await _mediator.Send(createUserCommand, cancellationToken);
        }
        else
        {
            var updateUserCommand = new UpdateUserCommand(
                id: user.GetId(),
                firstName: command.FirstName,
                lastName: command.LastName,
                birthday:command.Birthday,
                gender: command.Gender,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress,
                userType: UserType.Patient,
                isRegistrationCompleted: true
            );

            await _mediator.Send(updateUserCommand, cancellationToken);
        }

        var result = await new CreatePatientFlow(user, patientOptions, location, _dateTimeProvider.UtcNow()).Materialize(_materializeFlow);
        return result.Select<Patient>();
    }

    #endregion
}