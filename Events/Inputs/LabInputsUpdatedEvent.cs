using System;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Inputs;
using MediatR;

namespace WildHealth.Application.Events.Inputs
{
    public class LabInputsUpdatedEvent : INotification
    {
        public int PatientId { get; }
        public DateTime MostRecentLabDate { get; }
        public DateTime UpdatedAt { get; }
        public ICollection<LabInputValue> Inputs { get; }
        public ICollection<LabInput> CreatedInputs { get; }

        public LabInputsUpdatedEvent(
            int patientId,
            DateTime mostRecentLabDate,
            DateTime updatedAt,
            ICollection<LabInputValue> inputs,
            ICollection<LabInput> createdInputs)
        {
            PatientId = patientId;
            MostRecentLabDate = mostRecentLabDate;
            UpdatedAt = updatedAt;
            Inputs = inputs;
            CreatedInputs = createdInputs;
        }
    }
}