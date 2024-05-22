using MediatR;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Commands.Patients
{ 
    public class CheckAtRiskPatientTagsCommand:IRequest
    {
        public Patient Patient {get; }
        
        public CheckAtRiskPatientTagsCommand(Patient patient)
        {
            Patient = patient;
        }
    }
}