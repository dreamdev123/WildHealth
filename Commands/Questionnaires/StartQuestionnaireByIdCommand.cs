using WildHealth.Domain.Entities.Questionnaires;
using MediatR;

namespace WildHealth.Application.Commands.Questionnaires
{
    public class StartQuestionnaireByIdCommand : IRequest<QuestionnaireResult>
    {
        public int PatientId { get; }
        
        public int QuestionnaireId { get; }
        
        public int? AppointmentId { get; }
        
        public StartQuestionnaireByIdCommand(
            int patientId, 
            int questionnaireId,
            int? appointmentId = null)
        {
            PatientId = patientId;
            QuestionnaireId = questionnaireId;
            AppointmentId = appointmentId;
        }
    }
}