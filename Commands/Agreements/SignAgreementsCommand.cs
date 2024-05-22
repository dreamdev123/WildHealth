using MediatR;
using WildHealth.Common.Models.Agreements;
using WildHealth.Domain.Entities.Agreements;

namespace WildHealth.Application.Commands.Agreements
{
    public class SignAgreementsCommand : IRequest<AgreementConfirmation[]>
    {
        public int PatientId { get; }
        
        public ConfirmAgreementModel[] Confirmations { get; }
        
        public SignAgreementsCommand(
            int patientId,
            ConfirmAgreementModel[] confirmations)
        {
            PatientId = patientId;
            Confirmations = confirmations;
        }
    }
}