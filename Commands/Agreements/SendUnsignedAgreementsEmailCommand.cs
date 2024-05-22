using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Agreements
{
    public class SendUnsignedAgreementsEmailCommand : IRequest
    {
        public Patient Patient { get; }
        
        public SendUnsignedAgreementsEmailCommand(Patient patient)
        {
            Patient = patient;
        }
    }
}