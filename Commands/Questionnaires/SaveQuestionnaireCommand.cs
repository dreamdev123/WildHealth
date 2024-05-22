using System;
using System.Collections.Generic;
using MediatR;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.Questionnaires;

namespace WildHealth.Application.Commands.Questionnaires
{
    public class SaveQuestionnaireCommand : IRequest<QuestionnaireResult>
    {
        public Guid? PatientIntakeId { get; }

        public int? PatientId { get; }

        public int QuestionnaireResultId { get; }

        public DateTime? SubmittedAt { get; }

        public IEnumerable<AnswerModel> Answers { get; }

        public SaveQuestionnaireCommand(
            Guid intakeId,
            int questionnaireResultId,
            IEnumerable<AnswerModel> answers,
            DateTime? submittedAt)
        {
            PatientIntakeId = intakeId;
            QuestionnaireResultId = questionnaireResultId;
            SubmittedAt = submittedAt;
            Answers = answers;
        }
        
        public SaveQuestionnaireCommand(
            int patientId,
            int questionnaireResultId,
            IEnumerable<AnswerModel> answers,
            DateTime? submittedAt)
        {
            PatientId = patientId;
            QuestionnaireResultId = questionnaireResultId;
            SubmittedAt = submittedAt;
            Answers = answers;
        }
    }
}
