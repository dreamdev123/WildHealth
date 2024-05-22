using System.Collections.Generic;
using System.Linq;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Messages;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.CommandHandlers.AdminTools.Flows;

public class SendMarketingMessageFlow
{
    private readonly MessageType[] _messageTypes;
    private readonly string _subject;
    private readonly string _body;
    private readonly Employee _employee;
    private readonly MyPatientsFilterModel _filter;


    public SendMarketingMessageFlow(string subject,
        string body,
        MessageType[] messageTypes,
        MyPatientsFilterModel filter,
        Employee employee)
    {
        _subject = subject;
        _body = body;
        _messageTypes = messageTypes;
        _employee = employee;
        _filter = filter;
    }

    public SendMarketingMessageFlowResult Execute()
    {
        var messages = GetMessages().ToList();

        return new SendMarketingMessageFlowResult(messages);
    }

    private IEnumerable<Message> GetMessages()
    {
        foreach (var type in _messageTypes)
        {
            yield return new Message(
                subject: _subject,
                body: _body,
                type: type,
                audienceType: GetMessageAudience(),
                employee: _employee
            );
        }
    }

    private MessageAudienceType GetMessageAudience()
    {
        if (_filter.LastAppointmentGreaterThanDaysAgo is not null)
        {
            switch (_filter.LastAppointmentGreaterThanDaysAgo)
            {
                case 10:
                    return MessageAudienceType.LastAppointment10DaysAgo;
                case 20:
                    return MessageAudienceType.LastAppointment20DaysAgo;
                case 30:
                    return MessageAudienceType.LastAppointment30DaysAgo;
                case 60:
                    return MessageAudienceType.LastAppointment60DaysAgo;
            }
        }

        if (_filter.LastCoachingVisitGreaterThanDaysAgo is not null)
        {
            switch (_filter.LastCoachingVisitGreaterThanDaysAgo)
            {
                case 10:
                    return MessageAudienceType.LastCoachVisit10DaysAgo;
                case 20:
                    return MessageAudienceType.LastCoachVisit20DaysAgo;
                case 30:
                    return MessageAudienceType.LastCoachVisit30DaysAgo;
                case 60:
                    return MessageAudienceType.LastCoachVisit60DaysAgo;
            }
        }

        if (_filter.LastMessageSentGreaterThanDaysAgo is not null)
        {
            switch (_filter.LastMessageSentGreaterThanDaysAgo)
            {
                case 10:
                    return MessageAudienceType.LastMessageSent10DaysAgo;
                case 20:
                    return MessageAudienceType.LastMessageSent20DaysAgo;
                case 30:
                    return MessageAudienceType.LastMessageSent30DaysAgo;
                case 60:
                    return MessageAudienceType.LastMessageSent60DaysAgo;
            }
        }

        if (_filter.DaysSinceIccWithoutImcScheduledFromToday is not null)
        {
            switch (_filter.DaysSinceIccWithoutImcScheduledFromToday)
            {
                case 30:
                    return MessageAudienceType.SinceIccWithoutImc30Days;
                case 45:
                    return MessageAudienceType.SinceIccWithoutImc45Days;
                case 60:
                    return MessageAudienceType.SinceIccWithoutImc60Days;
            }
        }

        if (_filter.DaysSinceSignUpWithoutIccScheduledFromToday is not null)
        {
            switch (_filter.DaysSinceSignUpWithoutIccScheduledFromToday)
            {
                case 5:
                    return MessageAudienceType.SinceSignUpWithoutIcc5Days;
                case 10:
                    return MessageAudienceType.SinceSignUpWithoutIcc10Days;
                case 20:
                    return MessageAudienceType.SinceSignUpWithoutIcc20Days;
                case 30:
                    return MessageAudienceType.SinceSignUpWithoutIcc30Days;
            }
        }

        if (_filter.PlanRenewalDateLessThanDaysFromToday is not null)
        {
            switch (_filter.PlanRenewalDateLessThanDaysFromToday)
            {
                case 30:
                    return MessageAudienceType.PlanRenewalIn30Days;
                case 60:
                    return MessageAudienceType.PlanRenewalIn60Days;
            }
        }

        if (!string.IsNullOrEmpty(_filter.PlanName))
        {
            switch (_filter.PlanName.ToLower())
            {
                case "precision care package":
                    return MessageAudienceType.PrecisionCarePackagePlan;
                case "wild health light":
                    return MessageAudienceType.WildHealthLightPlan;
                case "single plan":
                    return MessageAudienceType.SinglePlan;
                case "advanced":
                    return MessageAudienceType.AdvancedPlan;
                case "optimization":
                    return MessageAudienceType.OptimizationPlan;
                case "core":
                    return MessageAudienceType.CorePlan;
                case "precision care":
                    return MessageAudienceType.PrecisionCarePlan;
                case "precision care health coaching":
                    return MessageAudienceType.PrecisionCareHealthCoachingPlan;
            }
        }

        if (_filter.IncludesTags is not null && _filter.IncludesTags.Length > 0)
        {
            var tag = _filter.IncludesTags.First();

            switch (tag)
            {
                case "Insurance Pending":
                    return MessageAudienceType.InsurancePending;
                case "Insurance Verified":
                    return MessageAudienceType.InsuranceVerified;
                case "IMC Due":
                    return MessageAudienceType.ImcDue;
                case "ICC Due":
                    return MessageAudienceType.IccDue;
                default:
                    return MessageAudienceType.CustomTag;
            }
        }

        return MessageAudienceType.All;
    }
}

public record SendMarketingMessageFlowResult(List<Message> MessagesToSend);