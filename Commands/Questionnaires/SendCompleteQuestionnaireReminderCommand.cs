using System;
using MediatR;

namespace WildHealth.Application.Commands.Questionnaires
{
    public class SendCompleteQuestionnaireReminderCommand : IRequest
    {
        public DateTime Date { get; }

        public SendCompleteQuestionnaireReminderCommand(DateTime date)
        {
            Date = date;
        }
    }
}