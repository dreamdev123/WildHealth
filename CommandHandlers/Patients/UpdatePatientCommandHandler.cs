using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Services.LeadSources;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Exceptions;
using System.Net;
using MediatR;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Twilio.Clients.Exceptions;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Patient>
    {
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeeService;
        private readonly ILeadSourcesService _leadSourceService;
        private readonly IConversationsService _conversationsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public UpdatePatientCommandHandler(
            IPatientsService patientsService,
            IEmployeeService employeeService,
            ILeadSourcesService leadSourceService,
            IConversationsService conversationsService,
            ITransactionManager transactionManager,
            IPermissionsGuard permissionsGuard,
            IMediator mediator,
            ILogger<UpdatePatientCommandHandler> logger)
        {
            _patientsService = patientsService;
            _employeeService = employeeService;
            _leadSourceService = leadSourceService;
            _conversationsService = conversationsService;
            _transactionManager = transactionManager;
            _permissionsGuard = permissionsGuard;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Patient> Handle(UpdatePatientCommand command, CancellationToken cancellationToken)
        {
            var spec = PatientSpecifications.UpdatePatientSpecification;
            
            var patient = await _patientsService.GetByIdAsync(command.Id, spec);

            _permissionsGuard.AssertPermissions(patient);

            var updateUserCommand = new UpdateUserCommand(
                id: patient.User.GetId(),
                firstName: command.FirstName,
                lastName: command.LastName,
                birthday: command.Birthday,
                gender: command.Gender,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress,
                userType: null
            );

            int[] newlyAssignedEmployeeIds;

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                if (command.LeadSource != null && patient.PatientLeadSource == null)
                {
                    //Create it.
                    var leadSource = await _leadSourceService.GetAsync(command.LeadSource.LeadSourceId, patient.User.PracticeId);
                    await _leadSourceService.CreatePatientLeadSourceAsync(patient, leadSource, command.LeadSource.OtherLeadSource, command.LeadSource.PodcastSource);
                }
                else if (patient.PatientLeadSource != null && command.LeadSource == null)
                {
                    //Delete it.
                    await _leadSourceService.DeletePatientLeadSourceAsync(patient.PatientLeadSource);
                }
                else if (command.LeadSource != null && patient.PatientLeadSource != null)
                {
                    //Are they the same? 
                    if (patient.PatientLeadSource.LeadSource.Id == command.LeadSource.LeadSourceId)
                    {
                    }
                    else
                    {
                        //Update it.
                        await _leadSourceService.DeletePatientLeadSourceAsync(patient.PatientLeadSource);
                        var leadSource = await _leadSourceService.GetAsync(command.LeadSource.LeadSourceId, patient.User.PracticeId);
                        await _leadSourceService.CreatePatientLeadSourceAsync(patient, leadSource, command.LeadSource.OtherLeadSource, command.LeadSource.PodcastSource);
                    }

                }
                else
                {
                    //The patient lead source is null, and so is the command lead source. 
                    //There is nothing to do.
                }

                var priorAssignedEmployeeIds = patient.GetAssignedEmployeesIds();

                await _mediator.Send(updateUserCommand, cancellationToken);

                await _patientsService.UpdatePatientOptionsAsync(patient, command.Options);

                newlyAssignedEmployeeIds = (await _patientsService.AssignToEmployeesAsync(patient, command.EmployeeIds)).ToArray();

                await AddAssignedEmployeesToHealthConversation(patient, newlyAssignedEmployeeIds, priorAssignedEmployeeIds);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            if (newlyAssignedEmployeeIds.Any()) {
                await _employeeService.GetByIdsAsync(newlyAssignedEmployeeIds, EmployeeSpecifications.ActiveWithUser);
            }

            await _mediator.Publish(new PatientUpdatedEvent(patient.GetId(), newlyAssignedEmployeeIds), cancellationToken);

            return patient;
        }

        private async Task AddAssignedEmployeesToHealthConversation(Patient patient, int[] newlyAssignedEmployeeIds, int[] priorAssignedEmployeeIds)
        {
            var assignedEmployeeIds = patient.GetAssignedEmployeesIds();
            
            try
            {
                var conversation = await GetHealthConversationAsync(patient, newlyAssignedEmployeeIds);

                // This will be null if no employees are assigned yet and because of that we cannot create a health conversation
                if (conversation is null)
                {
                    return;
                }
                var conversationDomain = ConversationDomain.Create(conversation);
                
                foreach (var participant in conversation.EmployeeParticipants)
                {
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Basically we only want to remove participants if they were previously an employee for the patient but no longer are after the updates
                    // This will prevent health coaches that were part of a conversation previously but weren't assigned as an employee to the patient from getting removed
                    // https://wildhealth.atlassian.net/browse/CLAR-2450
                    if (!assignedEmployeeIds.Contains(participant.EmployeeId) && priorAssignedEmployeeIds.Contains(participant.EmployeeId))
                    {
                        var command = new RemoveEmployeeParticipantFromConversationCommand(
                            conversationId: conversation.GetId(),
                            userId: participant.Employee.UserId
                        );

                        await _mediator.Send(command);
                    }
                    
                    /////////////////////////////////////////////////////////////////////////////////
                    // If participant came to conversation as delegated and those who delegated him
                    // is removed from the assignment list - we remove delegated employee as well
                    if (participant.DelegatedBy.HasValue
                        && !assignedEmployeeIds.Contains(participant.DelegatedBy.Value) 
                        && priorAssignedEmployeeIds.Contains(participant.DelegatedBy.Value))
                    {
                        var command = new RemoveEmployeeParticipantFromConversationCommand(
                            conversationId: conversation.GetId(),
                            userId: participant.Employee.UserId
                        );

                        await _mediator.Send(command);
                    }
                }

                foreach (var employeeId in assignedEmployeeIds)
                {
                    if (!conversationDomain.HasEmployeeParticipant(employeeId))
                    {
                        var command = new AddEmployeeParticipantToConversationCommand(
                            conversationId: conversation.GetId(),
                            employeeId: employeeId
                        );

                        var result = await _mediator.Send(command).ToTry();
                        
                        // If the participant already exists (TwilioException with HttpStatusCode.Conflict) then we want to just continue on
                        if (result.IsError() && result.Exception() is not TwilioException { StatusCode: HttpStatusCode.Conflict })
                        {
                            // Log error, then continue
                            var ex = result.Exception();
                            _logger.LogWarning(ex,
                                "There was a problem attempting to add [EmployeeId] = {EmployeeId} to [ConversationId] = {ConversationId}, [Message]: {Message}",
                                employeeId, conversation.GetId(), ex.Message);
                        }
                    }
                }
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                //Conversation not found then do nothing.
                _logger.LogError($"Problem assigning participants to conversation for [PatientId] = {patient.GetId()} - {ex.ToString()}");
            }
        }

        /// <summary>
        /// This is designed to either get the conversation if it already exists or if we are certain there are assigned
        /// employeeIds, then we should create the health care conversation
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="newlyAssignedEmployeeIds"></param>
        /// <returns></returns>
        private async Task<Conversation?> GetHealthConversationAsync(Patient patient, int[] newlyAssignedEmployeeIds)
        {
            var patientId = patient.GetId();
            
            if (newlyAssignedEmployeeIds.Any())
            {
                // If the conversation already exists this will only return the existing conversation
                return await _mediator.Send(new StartHealthCareConversationCommand(
                    employeeId: 0,
                    patientId: patientId,
                    practiceId: patient.User.PracticeId,
                    locationId: patient.LocationId
                ));
            }

            try
            {
                return await _conversationsService.GetHealthConversationByPatientAsync(patientId);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation($"Attempting to get conversation failed because it does not yet exist, employees must be assigned before it can exist");
                
                return null;
            }    
        }
    }
}