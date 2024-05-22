using System;
using MediatR;

namespace WildHealth.Application.Commands.Questionnaires
{
    public class SendQuestionnaireReminderCommand : IRequest
    {
        public DateTime Date { get; }

        public string SMSMessageTemplate {get; }

        public SendQuestionnaireReminderCommand(DateTime date)
        {
            Date = date;
            SMSMessageTemplate =  "Wild Health: Make sure you fill out your health forms - this is a key step before your first visit with us! {{applicationQuestionnaireUrl}}";
        }
    }
}
