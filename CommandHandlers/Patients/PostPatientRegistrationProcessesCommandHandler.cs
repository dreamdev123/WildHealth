using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Durable.Chain;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Founders;
using WildHealth.Application.Services.LeadSources;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.IntegrationEvents.Patients;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class PostPatientRegistrationProcessesCommandHandler : IRequestHandler<PostPatientRegistrationProcessesCommand>
    {
        private readonly IMediator _mediator;
        private readonly IDurableMediator _durableMediator;
        private readonly IPatientsService _patientsService;
        private readonly IEmployerProductService _employerProductService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly ILogger _logger;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ILeadSourcesService _leadSourceService;
        private readonly IFoundersService _foundersService;
        private readonly IEmployeeService _employeeService;
        private readonly ILocationsService _locationsService;
        private readonly IDurableChainOrchestrator _durableChainOrchestrator;
        
        public PostPatientRegistrationProcessesCommandHandler(
            IMediator mediator,
            IPatientsService patientsService,
            IEmployerProductService employerProductService,
            IFeatureFlagsService featureFlagsService,
            ILogger<PostPatientRegistrationProcessesCommandHandler> logger,
            IPaymentPlansService paymentPlansService,
            ILeadSourcesService leadSourceService,
            IFoundersService foundersService,
            IEmployeeService employeeService,
            ILocationsService locationsService, 
            IDurableChainOrchestrator durableChainOrchestrator, 
            IDurableMediator durableMediator)
        {
            _mediator = mediator;
            _patientsService = patientsService;
            _employerProductService = employerProductService;
            _featureFlagsService = featureFlagsService;
            _logger = logger;
            _paymentPlansService = paymentPlansService;
            _leadSourceService = leadSourceService;
            _foundersService = foundersService;
            _employeeService = employeeService;
            _locationsService = locationsService;
            _durableChainOrchestrator = durableChainOrchestrator;
            _durableMediator = durableMediator;
        }

        public async Task Handle(PostPatientRegistrationProcessesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started processing post registration processes of patient with [Id] = {command.PatientId}.");

            var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithAggregationInputs);
            var user = patient.User;
            var location = await _locationsService.GetByIdAsync(command.LocationId, command.PracticeId);
            var employerProduct = await GetEmployerProductAsync(command.EmployerProductKey);
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(command.PaymentPriceId);
            var isTrialPlan = paymentPrice.PaymentPeriod.PaymentPlan.IsTrial;

            await HandleCore(command, chain =>
            {
                chain.Pipe("ApplyCouponProducts", async _ =>
                {
                    await _mediator.Send(new ApplyCouponProductsCommand(patient, paymentPrice.GetId()), cancellationToken);
                });

                if (!isTrialPlan)
                {
                    chain.Pipe("CreateBuiltInProducts", async _ =>
                    {
                        var createBuildInProductsCommand = new CreateBuiltInProductsCommand(subscriptionId: command.SubscriptionId);
                        await _mediator.Send(createBuildInProductsCommand, cancellationToken);
                    });
                    
                    chain.Pipe("BuyAddOns", async _ =>
                    {
                        var buyAddOnsCommand = new BuyAddOnsCommand(
                            patient: patient,
                            selectedAddOnIds: command.AddonIds,
                            paymentPriceId: command.PaymentPriceId,
                            buyRequiredAddOns: true,
                            practiceId: command.PracticeId,
                            skipPaymentError: false,
                            employerProduct: employerProduct
                        );

                        await _mediator.Send(buyAddOnsCommand, cancellationToken);
                    });
                }

                if (command.EmployeeId.HasValue)
                {
                    chain.Pipe("SendFellowshipPatientEnrollment", async _ =>
                    {
                        await SendFellowshipPatientEnrollmentNotificationAsync(command.PracticeId, command.LocationId);
                    });
                }

                if (command.LinkedEmployeeId.HasValue)
                {
                    chain.Pipe("SendFellowshipPatientEnrollmentNotification", async _ =>
                    {
                        await SendFellowEnrollmentNotificationAsync(command.PracticeId, command.LocationId);
                    });
                }

                if (paymentPrice.IsInsurance())
                {
                    chain.Pipe("CreateInsurancePendingTag", async _ =>
                    {
                        await _mediator.Send(new CreateTagCommand(patient, Common.Constants.Tags.InsurancePending), cancellationToken);
                    });

                    chain.Pipe("UpdateFhirPatient", async _ =>
                    {
                        await _mediator.Send(new UpdateFhirPatientCommand(patient.GetId()), cancellationToken);
                    });

                    chain.Pipe("RunVerificationCheck", async _ =>
                    {
                        await _mediator.Send(new RunInsuranceVerificationCommand(patient.GetId()), cancellationToken);
                    });
                }
                
                // If they are a fellow, we want to tag them here
                // https://wildhealth.atlassian.net/browse/CLAR-2483
                if (IsFellow(command))
                {
                    chain.Pipe("CreateFellowshipPatientTag", async _ =>
                    {
                        await _mediator.Send(new CreateTagCommand(patient, Common.Constants.Tags.FellowshipPatient), cancellationToken);
                    });
                }
                
                if (employerProduct is not null && !employerProduct.IsDefault)
                {
                    chain.Pipe("CreateEmployerProductTag", async _ =>
                    {
                        await _mediator.Send(new CreateTagCommand(patient, GetTagForEmployer(employerProduct)!), cancellationToken);
                    });
                }

                if (command.LeadSourceId.HasValue)
                {
                    chain.Pipe("CreatePatientLeadSource", async e =>
                    {
                        var leadSource = await _leadSourceService.GetAsync(command.LeadSourceId.Value, command.PracticeId);
                        await _leadSourceService.CreatePatientLeadSourceAsync(patient, leadSource, command.OtherLeadSource, command.PodcastSource);
                    });
                }

                chain.Pipe("LinkToEmployee", async _ =>
                {
                    await AssignEmployeesAsync(patient, command.FounderId, command.EmployeeId);
                    await LinkToEmployeeAsync(patient, command.LinkedEmployeeId);
                });

                chain.Pipe("CompleteOnBoarding", async _ =>
                {
                    await CreateLmsRecord(patient.User);
                });

                chain.Pipe("PublishPatientCreatedEvent", async _ =>
                {
                    var patientCreatedEvent = new PatientCreatedEvent(
                        PatientId: patient.GetId(),
                        SubscriptionId: command.SubscriptionId,
                        SelectedAddOnIds: command.AddonIds.ToArray());

                    await _durableMediator.Publish(patientCreatedEvent);
                });

                chain.Pipe("AddPatientAggregationInput", async _ =>
                {
                    var aggregationInputCommand = new AddPatientAggregationInputCommand(
                        patientId: patient.GetId()
                    );

                    await _mediator.Send(aggregationInputCommand, cancellationToken);
                });
            });

            _logger.LogInformation($"Finish to process post registration processes of patient with [Id] = {command.PatientId}.");
        }
        
        #region private

        private async Task HandleCore(PostPatientRegistrationProcessesCommand command, Action<ChainOfResponsibility<PatientIntegrationEvent>> chain)
        {
            await _durableChainOrchestrator.Run(
                payload: command.OriginatedFromEvent,
                startAtStep: command.OriginatedFromEvent.State,
                chainBuilder: chain);
        } 
        
        private string? GetTagForEmployer(EmployerProduct employerProduct)
        {
            return employerProduct.Key;
        }

        private async Task CreateLmsRecord(User user)
        {
            var command = new CreateLearnerFromUserCommand(user);
            await _mediator.Send(command);
        }
        
        private async Task SendFellowshipPatientEnrollmentNotificationAsync(int practiceId, int locationId)
        {
            var command = new SendFellowshipPatientEnrollmentNotificationCommand(practiceId, locationId);

            await _mediator.Send(command);
        }

        private async Task SendFellowEnrollmentNotificationAsync(int practiceId, int locationId)
        {
            var command = new SendFellowEnrollmentNotificationCommand(practiceId, locationId);

            await _mediator.Send(command);
        }
        
        private async Task<EmployerProduct> GetEmployerProductAsync(string employerProductKey)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
            {
                return await _employerProductService.GetByKeyAsync();
            }
            
            return await _employerProductService.GetByKeyAsync(employerProductKey);
        }
        
        private async Task AssignEmployeesAsync(Patient patient, int? founderId, int? employeeId)
        {
            var employeeIds = patient.GetAssignedEmployeesIds().ToList();

            if (founderId.HasValue)
            {
                var founder = await _foundersService.GetByIdAsync(founderId.Value);

                employeeIds.Add(founder.EmployeeId);
            }

            if (employeeId.HasValue)
            {
                var employee = await _employeeService.GetByIdAsync(employeeId.Value);

                employeeIds.Add(employee.GetId());
            }

            if (!employeeIds.Any())
            {
                return;
            }

            await _patientsService.AssignToEmployeesAsync(patient, employeeIds.Distinct().ToArray());
        }

        private async Task LinkToEmployeeAsync(Patient patient, int? employeeId)
        {
            if (employeeId.HasValue)
            {
                await _patientsService.LinkToEmployeeAsync(patient, employeeId.Value);
            }
        }

        private bool IsFellow(PostPatientRegistrationProcessesCommand command)
        {
            if (command.EmployeeId.HasValue && command.LinkedEmployeeId.HasValue)
            {
                return command.EmployeeId.Value != 0 || command.LinkedEmployeeId.Value != 0;
            }

            if (command.EmployeeId.HasValue)
            {
                return command.EmployeeId.Value != 0;
            }

            if (command.LinkedEmployeeId.HasValue)
            {
                return command.LinkedEmployeeId.Value != 0;
            }

            return false;
        }

        #endregion
    }
}
