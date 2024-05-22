using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Cloners.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Utils.ArchiveEmailCreator;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Events.Patients;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class ChangePracticeCopyCommandHandler : IRequestHandler<ChangePracticeCopyCommand, Patient>
    {
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;
        private readonly IPracticeService _practiceService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPatientCloner _patientCloner;
        private readonly IArchiveEmailCreator _archiveEmailCreator;
        private readonly ILogger _logger;

        public ChangePracticeCopyCommandHandler(
            IMediator mediator,
            IPatientsService patientsService,
            IPracticeService practiceService,
            ITransactionManager transactionManager,
            IPatientCloner patientCloner,
            IArchiveEmailCreator archiveEmailCreator,
            ILogger<ChangePracticeCopyCommandHandler> logger)
        {
            _mediator = mediator;
            _patientsService = patientsService;
            _practiceService = practiceService;
            _transactionManager = transactionManager;
            _patientCloner = patientCloner;
            _archiveEmailCreator = archiveEmailCreator;
            _logger = logger;
        }
        
        public async Task<Patient> Handle(ChangePracticeCopyCommand command, CancellationToken cancellationToken)
        {
            var clonePatient = await _patientsService.GetByIdForCloneAsync(command.PatientId);
            var toPractice = await _practiceService.GetAsync(command.ToPracticeId, PracticeSpecifications.LocationSpecification);

            // Perform initial validations
            AssertPatientCanBeCopied(clonePatient, toPractice);

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                _logger.LogInformation($"Starting to copy patient with [Id] = {command.PatientId} to [PracticeId] = {command.ToPracticeId}.");
                
                var email = clonePatient.User.Email;
                var practiceId = clonePatient.User.PracticeId;

                // Change the email of the prior patient
                var oldEmail = _archiveEmailCreator.GenerateArchivedEmailNameForOldPractice(email, practiceId);
                clonePatient.User.Email = oldEmail;
                clonePatient.User.Identity.Email = oldEmail;
                await _patientsService.UpdateAsync(clonePatient);

                // Create the clone of the new patient
                var clone = await _patientCloner.ClonePatientForNewPracticeAsync(clonePatient, email, toPractice);
                var result = clone.User.Email;

                await transaction.CommitAsync(cancellationToken);

                var oldPatient = await _patientsService.GetByIdAsync(command.PatientId);

                var patientMovedEvent = new PatientMovedEvent(
                    oldPatient: oldPatient,
                    newPatient: clone);

                await _mediator.Publish(patientMovedEvent, cancellationToken);

                return clonePatient;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error copying patient with [Id] = {command.PatientId} to [PracticeId] = {command.ToPracticeId} - {ex.Message}");
                
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }
        
        private void AssertPatientCanBeCopied(Patient patient, Practice practice)
        {
            // Verify that they have no active subscriptions
            var activeSubscriptions = patient.Subscriptions.Where(o => o.GetStatus().Equals(SubscriptionStatus.Active));
            if(activeSubscriptions.Any())
            {
                var patientIdParam = new AppException.ExceptionParameter("PatientId", patient.GetId());
                var practiceIdParam = new AppException.ExceptionParameter("PracticeId", practice.GetId());
                var activeSubscriptionIdParam = new AppException.ExceptionParameter("SubscriptionId", activeSubscriptions.First().GetId());
                throw new AppException(System.Net.HttpStatusCode.BadRequest, "Unable to copy patient to practice because they have at least one active subscription", patientIdParam, practiceIdParam, activeSubscriptionIdParam);
            }

            // Verify that they have no upcoming appointments
            var upcomingAppointments = patient.Appointments.Where(o => o.StartDate >= DateTime.Now);
            if(upcomingAppointments.Any())
            {
                var patientIdParam = new AppException.ExceptionParameter("PatientId", patient.GetId());
                var practiceIdParam = new AppException.ExceptionParameter("PracticeId", practice.GetId());
                var upcomingAppointmentIdParam = new AppException.ExceptionParameter("AppointmentId", upcomingAppointments.First().GetId());
                throw new AppException(System.Net.HttpStatusCode.BadRequest, "Unable to copy patient to practice because they have at least one upcoming appointment", patientIdParam, practiceIdParam, upcomingAppointmentIdParam);
            }
        }
    }
}