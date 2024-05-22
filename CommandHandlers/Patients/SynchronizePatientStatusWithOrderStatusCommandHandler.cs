using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.Patient;
using MediatR;
using WildHealth.Domain.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SynchronizePatientStatusWithOrderStatusCommandHandler : IRequestHandler<SynchronizePatientStatusWithOrderStatusCommand>
    {
        private readonly IPatientsService _patientsService;

        public SynchronizePatientStatusWithOrderStatusCommandHandler(IPatientsService patientsService)
        {
            _patientsService = patientsService;
        }

        public async Task Handle(SynchronizePatientStatusWithOrderStatusCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;

            switch (order.Type)
            {
                case OrderType.Dna: await UpdatePatientDnaStatusAsync(order.PatientId, order.Status); break;
                case OrderType.Lab: await UpdatePatientLabsStatusAsync(order.PatientId, order.Status); break;
                case OrderType.Epigenetic: await UpdatePatientEpigeneticStatusAsync(order.PatientId, order.Status); break;
                case OrderType.Referral:  break;
                case OrderType.Other:  break;

                default: throw new ArgumentException("Unsupported order type.");
            }
        }

        #region private

        private async Task UpdatePatientDnaStatusAsync(int patientId, OrderStatus status)
        {
            var patient = await _patientsService.GetByIdAsync(patientId);

            switch (status)
            {
                case OrderStatus.Ordered: patient.DnaStatus = PatientDnaStatus.Preparing; break;
                case OrderStatus.Placed: patient.DnaStatus = PatientDnaStatus.InProgress; break;
                case OrderStatus.ManualFlow: patient.DnaStatus = PatientDnaStatus.InProgress; break;
                case OrderStatus.Shipping: patient.DnaStatus = PatientDnaStatus.InProgress; break;
                case OrderStatus.Completed: patient.DnaStatus = PatientDnaStatus.Completed; break;
                case OrderStatus.Failed: patient.DnaStatus = PatientDnaStatus.Failed; break;

                default: throw new ArgumentException("Unsupported patient add-on status");
            }

            await UpdatePatientAsync(patient, nameof(patient.DnaStatus));
        }

        private async Task UpdatePatientLabsStatusAsync(int patientId, OrderStatus status)
        {
            var patient = await _patientsService.GetByIdAsync(patientId);

            // If the labs have already resulted, we do not want to move it back to a prior state
            if(patient.LabsStatus == PatientLabsStatus.Resulted)
            {
                return;
            }

            switch (status)
            {
                case OrderStatus.Ordered: patient.LabsStatus = PatientLabsStatus.Preparing; break;
                case OrderStatus.Cancelled: patient.LabsStatus = PatientLabsStatus.Preparing; break;
                case OrderStatus.Placed: patient.LabsStatus = PatientLabsStatus.Ordered; break;
                case OrderStatus.Shipping: patient.LabsStatus = PatientLabsStatus.InProgress; break;
                case OrderStatus.Completed: patient.LabsStatus = PatientLabsStatus.Resulted; break;
                case OrderStatus.Failed: patient.LabsStatus = PatientLabsStatus.Resulted; break;

                default: throw new ArgumentException("Unsupported patient add-on status");
            }

            await UpdatePatientAsync(patient, nameof(patient.LabsStatus));
        }

        private async Task UpdatePatientEpigeneticStatusAsync(int patientId, OrderStatus status)
        {
            var patient = await _patientsService.GetByIdAsync(patientId);

            switch (status)
            {
                case OrderStatus.Ordered: patient.EpigeneticStatus = PatientEpigeneticStatus.Preparing; break;
                case OrderStatus.Placed: patient.EpigeneticStatus = PatientEpigeneticStatus.InProgress; break;
                case OrderStatus.Shipping: patient.EpigeneticStatus = PatientEpigeneticStatus.InProgress; break;
                case OrderStatus.Completed: patient.EpigeneticStatus = PatientEpigeneticStatus.Completed; break;
                case OrderStatus.Failed: patient.EpigeneticStatus = PatientEpigeneticStatus.Failed; break;

                default: throw new ArgumentException("Unsupported patient add-on status");
            }

            await UpdatePatientAsync(patient, nameof(patient.EpigeneticStatus));
        }

        private async Task UpdatePatientAsync(Patient patient, string statusName)
        {
            try
            {
                await _patientsService.UpdateAsync(patient);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.FirstOrDefault(x => x.Entity is Patient entity && entity.Id == patient.Id);

                if (entry is not null)
                {
                    var databaseValues = entry.GetDatabaseValues()!;
                    var currentValues = entry.CurrentValues;

                    foreach (var property in currentValues.Properties)
                    {
                        if (property.Name != statusName)
                        {
                            currentValues[property] = databaseValues[property];
                        }
                    }

                    entry.OriginalValues.SetValues(databaseValues);

                    await _patientsService.UpdateAsync(patient);
                }

            }
        }

        #endregion
    }
}
