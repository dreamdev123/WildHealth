using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Users;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Shortcuts;
using WildHealth.Domain.Entities.Medication;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Users
{
    public class ResetTestUsersToInitialStateCommandHandler : IRequestHandler<ResetTestUsersToInitialStateCommand, string[]>
    {
        private readonly IGeneralRepository<User> _usersRepository;
        private readonly IGeneralRepository<Patient> _patientsRepository;
        private readonly IGeneralRepository<Employee> _employeesRepository;
        private readonly IGeneralRepository<DnaOrder> _dnaOrdersRepository;
        private readonly IGeneralRepository<LabOrder> _labOrdersRepository;
        private readonly IGeneralRepository<EpigeneticOrder> _epigeneticOrdersRepository;
        private readonly IGeneralRepository<PatientMedication> _medicationsRepository;
        private readonly IGeneralRepository<PatientSupplement> _supplementsRepository;
        private readonly IGeneralRepository<Subscription> _subscriptionsRepository;
        private readonly IGeneralRepository<HealthReport> _healthReportsRepository;
        private readonly IGeneralRepository<Appointment> _appointmentsRepository;
        private readonly IGeneralRepository<ShortcutGroup> _shortCutGroupsRepository;
        private readonly IGeneralRepository<Note> _notesRepository;
        private readonly ILogger _logger;

        public ResetTestUsersToInitialStateCommandHandler(
            IGeneralRepository<User> usersRepository, 
            IGeneralRepository<Patient> patientsRepository, 
            IGeneralRepository<Employee> employeesRepository, 
            IGeneralRepository<DnaOrder> dnaOrdersRepository,
            IGeneralRepository<LabOrder> labOrdersRepository,
            IGeneralRepository<EpigeneticOrder> epigeneticOrdersRepository,
            IGeneralRepository<PatientMedication> medicationsRepository, 
            IGeneralRepository<PatientSupplement> supplementsRepository, 
            IGeneralRepository<Subscription> subscriptionsRepository, 
            IGeneralRepository<HealthReport> healthReportsRepository, 
            IGeneralRepository<Appointment> appointmentsRepository, 
            IGeneralRepository<ShortcutGroup> shortCutGroupsRepository, 
            IGeneralRepository<Note> notesRepository, 
            ILogger<ResetTestUsersToInitialStateCommandHandler> logger)
        {
            _usersRepository = usersRepository;
            _patientsRepository = patientsRepository;
            _employeesRepository = employeesRepository;
            _dnaOrdersRepository = dnaOrdersRepository;
            _labOrdersRepository = labOrdersRepository;
            _epigeneticOrdersRepository = epigeneticOrdersRepository;
            _medicationsRepository = medicationsRepository;
            _supplementsRepository = supplementsRepository;
            _subscriptionsRepository = subscriptionsRepository;
            _healthReportsRepository = healthReportsRepository;
            _appointmentsRepository = appointmentsRepository;
            _shortCutGroupsRepository = shortCutGroupsRepository;
            _notesRepository = notesRepository;
            _logger = logger;
        }

        public async Task<string[]> Handle(ResetTestUsersToInitialStateCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Started resetting test users to initial state. Received {command.Emails.Length} emails");

            var resetEmails = new List<string>();

            foreach (var email in command.Emails)
            {
                try
                {
                    _logger.LogInformation($"Started resetting test user with email: {email} to initial state");
                    
                    var result = await ResetUserToInitialStateAsync(email);

                    if (result)
                    {
                        resetEmails.Add(email);
                    }
                    
                    _logger.LogInformation($"Finished resetting test user with email: {email} to initial state");

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during resetting test user with email: {email} to initial state. {ex}");
                }
            }
            
            _logger.LogInformation($"Finished resetting test users to initial state. Reset {resetEmails.Count} emails");

            return resetEmails.ToArray();
        }
        
        #region private

        /// <summary>
        /// Resets user to initial state
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<bool> ResetUserToInitialStateAsync(string email)
        {
            var user = await _usersRepository
                .Get(x => x.Email == email)
                .Include(x => x.Identity)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (user is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Test user with email: {email} does not exist");
            }

            return user.Identity.Type switch
            {
                UserType.Employee => await ResetEmployeeToInitialStateAsync(user),
                UserType.Patient => await ResetPatientToInitialStateAsync(user),
                _ => throw new ArgumentException("Unsupported user type"),
            };
        }

        /// <summary>
        /// Resets patient to initial state by removing:
        ///     * DNA orders
        ///     * Lab orders
        ///     * Epigenetic orders
        ///     * Supplements
        ///     * Medications
        ///     * Cancelled subscriptions
        ///     * Appointments
        ///     * Health Reports
        ///     * Notes
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<bool> ResetPatientToInitialStateAsync(User user)
        {
            var patient = await _patientsRepository
                .All()
                .AsNoTracking()
                .ByUserId(user.GetId())
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter("UserId", user.GetId());
                throw new AppException(HttpStatusCode.NotFound, "Test patient for user does not exist", exceptionParam);
            }

            await ResetDnaOrderAsync(patient);

            await ResetLabOrderAsync(patient);

            await ResetEpigeneticOrdersAsync(patient);

            await ResetPatientSupplementsAsync(patient);
            
            await ResetPatientMedicationsAsync(patient);
            
            await ResetPatientSubscriptionsAsync(patient);

            await ResetPatientAppointmentsAsync(patient);

            await ResetPatientHealthReportsAsync(patient);

            await ResetPatientNotesAsync(patient);

            return true;
        }

        /// <summary>
        /// Removes DNA orders related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetDnaOrderAsync(Patient patient)
        {
            var orders = await _dnaOrdersRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeOrderItems()
                .ToArrayAsync();

            if (!orders.Any())
            {
                return;
            }
            
            foreach (var order in orders)
            {
                foreach (var orderItem in order.Items)
                {
                    _dnaOrdersRepository.DeleteRelated(orderItem);
                }
                
                _dnaOrdersRepository.Delete(order);
            }

            await _dnaOrdersRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes lab orders related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetLabOrderAsync(Patient patient)
        {
            var orders = await _labOrdersRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeOrderItems()
                .ToArrayAsync();

            if (!orders.Any())
            {
                return;
            }
            
            foreach (var order in orders)
            {
                foreach (var orderItem in order.Items)
                {
                    _labOrdersRepository.DeleteRelated(orderItem);
                }
                
                _labOrdersRepository.Delete(order);
            }

            await _labOrdersRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes epigenetic orders related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetEpigeneticOrdersAsync(Patient patient)
        {
            var orders = await _epigeneticOrdersRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeOrderItems()
                .ToArrayAsync();

            if (!orders.Any())
            {
                return;
            }
            
            foreach (var order in orders)
            {
                foreach (var orderItem in order.Items)
                {
                    _epigeneticOrdersRepository.DeleteRelated(orderItem);
                }
                
                _epigeneticOrdersRepository.Delete(order);
            }

            await _epigeneticOrdersRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes supplements related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientSupplementsAsync(Patient patient)
        {
            var supplements = await _supplementsRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .ToArrayAsync();

            if (!supplements.Any())
            {
                return;
            }
            
            foreach (var supplement in supplements)
            {
                _supplementsRepository.Delete(supplement);
            }

            await _supplementsRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes medications related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientMedicationsAsync(Patient patient)
        {
            var medications = await _medicationsRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .ToArrayAsync();

            if (!medications.Any())
            {
                return;
            }
            
            foreach (var medication in medications)
            {
                _medicationsRepository.Delete(medication);
            }

            await _medicationsRepository.SaveAsync();
        }

        /// <summary>
        /// Removes cancelled subscriptions with agreements related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientSubscriptionsAsync(Patient patient)
        {
            var subscriptions = await _subscriptionsRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeCancellationRequest()
                .IncludeAgreements()
                .ToArrayAsync();

            var cancelledSubscriptions = subscriptions
                .Where(x => !x.NotCanceled())
                .ToArray();

            if (!cancelledSubscriptions.Any())
            {
                return;
            }
            
            foreach (var subscription in cancelledSubscriptions)
            {
                foreach (var agreement in subscription.Agreements)
                {
                    _subscriptionsRepository.DeleteRelated(agreement);
                }
                
                _subscriptionsRepository.Delete(subscription);
            }

            await _subscriptionsRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes appointments related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientAppointmentsAsync(Patient patient)
        {
            var appointments = await _appointmentsRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeEmployee()
                .ToArrayAsync();

            if (!appointments.Any())
            {
                return;
            }
            
            foreach (var appointment in appointments)
            {
                foreach (var employee in appointment.Employees)
                {
                    _appointmentsRepository.DeleteRelated(employee);
                }
                
                _appointmentsRepository.Delete(appointment);
            }

            await _appointmentsRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes all health reports including sub reports related to corresponding patient except default report
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientHealthReportsAsync(Patient patient)
        {
            var healthReports = await _healthReportsRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeSubReports()
                .ToArrayAsync();

            var defaultReport = healthReports.FirstOrDefault(x => x.IsSubmitted() || x.Status.Status == HealthReportStatus.Preparing);

            var otherReports = healthReports.Except(new [] { defaultReport }).ToArray();

            if (!otherReports.Any())
            {
                return;
            }
            
            foreach (var healthReport in otherReports)
            {
                if (healthReport is null) continue;
                _healthReportsRepository.DeleteRelated(healthReport.AlphaReport);
                _healthReportsRepository.DeleteRelated(healthReport.OverallReport);
                _healthReportsRepository.DeleteRelated(healthReport.DietAndNutritionReport);
                _healthReportsRepository.DeleteRelated(healthReport.ExerciseAndRecoveryReport);
                _healthReportsRepository.DeleteRelated(healthReport.SleepReport);
                _healthReportsRepository.DeleteRelated(healthReport.NeurobehavioralReport);
                _healthReportsRepository.DeleteRelated(healthReport.MicrobiomeReport);
                _healthReportsRepository.DeleteRelated(healthReport.LongevityReport);
                _healthReportsRepository.DeleteRelated(healthReport.LabReport);
                _healthReportsRepository.Delete(healthReport);
            }

            await _healthReportsRepository.SaveAsync();
        }
        
        /// <summary>
        /// Removes all patient notes with content related to corresponding patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task ResetPatientNotesAsync(Patient patient)
        {
            var notes = await _notesRepository
                .All()
                .AsNoTracking()
                .RelatedToPatient(patient.GetId())
                .IncludeContent()
                .ToArrayAsync();
            
            if (!notes.Any())
            {
                return;
            }
            
            foreach (var note in notes)
            {
                _notesRepository.DeleteRelated(note.Content);
                _notesRepository.Delete(note);
            }

            await _notesRepository.SaveAsync();
        }

        /// <summary>
        /// Resets employee to initial state by removing:
        ///     * Shortcuts
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<bool> ResetEmployeeToInitialStateAsync(User user)
        {
            var employee = await _employeesRepository
                .All()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.GetId());

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter("UserId", user.GetId());
                throw new AppException(HttpStatusCode.NotFound, "Test employee for user does not exist", exceptionParam);
            }

            await ResetShortcutsAsync(employee);

            return true;
        }

        /// <summary>
        /// Removes all short cut groups with nested shortcuts which were created by corresponding employee
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        private async Task ResetShortcutsAsync(Employee employee)
        {
            var shortCutGroups = await _shortCutGroupsRepository
                .All()
                .AsNoTracking()
                .Where(x => x.CreatedBy == employee.UserId)
                .IncludeShortcuts()
                .ToArrayAsync();

            if (!shortCutGroups.Any())
            {
                return;
            }

            foreach (var group in shortCutGroups)
            {
                foreach (var shortCut in group.Shortcuts)
                {
                    _shortCutGroupsRepository.DeleteRelated(shortCut);
                }
                
                _shortCutGroupsRepository.Delete(group);
            }

            await _shortCutGroupsRepository.SaveAsync();
        }
        
        #endregion
    }
}