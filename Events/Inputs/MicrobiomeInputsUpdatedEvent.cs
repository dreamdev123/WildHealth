using System.Collections.Generic;
using MediatR;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Events.Inputs;

public record MicrobiomeInputsUpdatedEvent(int PatientId, ICollection<MicrobiomeInput> Inputs) : INotification
{
    
}