using System;
using System.Data.Entity;
using System.Linq;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Domain.Enums.Payments;
using static WildHealth.Domain.Enums.Appointments.AppointmentWithType;

namespace WildHealth.Application.Domain.PatientEngagements;

public class EngagementCriteriaQueryBuilder
{
    private IQueryable<Subscription> _source;
    private readonly DateTime _timestamp;
    private readonly EngagementCriteria _criteria;

    private static readonly string[] SingleStandardPremiumPlanNames =
        PremiumPaymentPlan.Names.Concat(new[] { PlanNames.Standard, PlanNames.SinglePlan }).ToArray();

    public EngagementCriteriaQueryBuilder(
        IQueryable<Subscription> source,
        DateTime timestamp,
        EngagementCriteria criteria)
    {
        _source = source;
        _timestamp = timestamp;
        _criteria = criteria;
    }

    public IQueryable<EngagementScannerResult> Build()
    {
        _source = _criteria.SubscriptionTier switch
        {
            SubscriptionTier.Premium => _source.Where(s =>
                PremiumPaymentPlan.Names.Contains(s.PaymentPrice.PaymentPeriod.PaymentPlan.Name)),
            SubscriptionTier.Regular => _source.Where(s =>
                !PremiumPaymentPlan.Names.Contains(s.PaymentPrice.PaymentPeriod.PaymentPlan.Name)),
            SubscriptionTier.RegularAndPremium => _source.Where(s =>
                SingleStandardPremiumPlanNames.Contains(s.PaymentPrice.PaymentPeriod.PaymentPlan.Name)),
            _ => _source
        };

        _source = _criteria.Assignee switch
        {
            EngagementAssignee.Patient => _source.Where(s =>
                !s.Patient.PatientEngagements.Any(pe =>
                    pe.ExpirationDate.Date >= _timestamp.Date && // not expired
                    pe.EngagementCriteria.Assignee == EngagementAssignee.Patient &&
                    pe.Status != PatientEngagementStatus.Completed)), // Completed do not count because they're required for Resurrection (when appointment booked but then cancelled)
            _ => _source
        };

        return _source
            .Where(s =>
                s.EndDate > _timestamp && s.CanceledAt == null && // active subscription
                s.Patient.User.PracticeId == _criteria.PracticeId &&
                !s.Patient.PatientEngagements.Any(pe =>
                    pe.EngagementCriteriaId == _criteria.Id &&
                    pe.CreatedAt.AddDays(_criteria.RepeatInDays) <= _timestamp.Date)) // current criteria hasn't been around for {RepeatInDays} days
            .Select(s => new EngagementScannerResult(s.Patient.Id!.Value, s.Patient.User.UniversalId,
                PremiumPaymentPlan.Names.Contains(s.PaymentPrice.PaymentPeriod.PaymentPlan.Name)))
            .AsNoTracking();
    }

    public EngagementCriteriaQueryBuilder MonthsSinceLastVisit(int months, params AppointmentWithType[] types)
    {
        _source = _source.Where(s =>
            s.Patient.Appointments
                .Any(a =>
                    types.Contains(a.WithType) &&
                    a.Status == AppointmentStatus.Submitted &&
                    !a.IsNoShow) &&
            s.Patient.Appointments
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault(a =>
                    types.Contains(a.WithType) &&
                    a.Status == AppointmentStatus.Submitted &&
                    !a.IsNoShow)!.StartDate.AddMonths(months).Date < _timestamp.Date);

        return this;
    }

    public EngagementCriteriaQueryBuilder DaysAfterHCAssigned(int days)
    {
        _source = _source
            .Where(s => s.Patient.Employees.Any(e =>
                e.Employee.RoleId == Roles.CoachId &&
                e.AssignedAt.Date.AddDays(days) < _timestamp.Date &&
                e.DeletedAt == null));

        return this;
    }

    public EngagementCriteriaQueryBuilder NoDnaResults()
    {
        _source = _source
            .Where(s =>
                s.Patient.DnaStatus != PatientDnaStatus.Completed &&
                s.Patient.Orders.Any(o => o.Type == OrderType.Dna));

        return this;
    }

    public EngagementCriteriaQueryBuilder TimeForLabs(EngagementDate days)
    {
        var (minDate, maxDate) = days.ToDate(_timestamp);

        _source = _source
            .Where(s =>
                s.Patient.Orders.Any(x =>
                    x.Type == OrderType.Lab &&
                    (minDate == null || x.ExpectedCollectionDate >= minDate) &&
                    (maxDate == null || x.ExpectedCollectionDate <= maxDate)));

        return this;
    }
    
    public EngagementCriteriaQueryBuilder DaysAfterDnaAndLabsReturned(int days)
    {
        _source = _source
            .Where(s =>
                s.Patient.LabsStatus == PatientLabsStatus.Resulted &&
                s.Patient.DnaStatus == PatientDnaStatus.Completed &&
                s.Patient.Orders
                    .Where(o => (o.Type == OrderType.Dna || o.Type == OrderType.Lab) && o.CompletedAt.HasValue)
                    .All(o => o.CompletedAt!.Value.Date.AddDays(days) < _timestamp.Date));

        return this;
    }

    public EngagementCriteriaQueryBuilder NoVisitDaysAfterDnaAndLabsReturned(int days,
        params AppointmentWithType[] types)
    {
        // orders completed more than N days ago
        DaysAfterDnaAndLabsReturned(days);

        // no appointments booked since last order completed
        _source = _source.Where(s => !s.Patient.Appointments.Any(a =>
            types.Contains(a.WithType) &&
            a.Status == AppointmentStatus.Submitted &&
            !a.IsNoShow &&
            a.StartDate > s.Patient.Orders
                .Where(o => (o.Type == OrderType.Dna || o.Type == OrderType.Lab) && o.CompletedAt.HasValue)
                .OrderByDescending(o => o.CompletedAt!.Value)
                .FirstOrDefault()!.CompletedAt));

        return this;
    }

    public EngagementCriteriaQueryBuilder NoVisit(params AppointmentWithType[] types)
    {
        _source = _source.Where(s => !s.Patient.Appointments.Any(a =>
            types.Contains(a.WithType) &&
            a.Status == AppointmentStatus.Submitted &&
            !a.IsNoShow));

        return this;
    }

    public EngagementCriteriaQueryBuilder NoNewVisit(int sinceDaysAgo, params AppointmentWithType[] types)
    {
        var minDate = _timestamp.AddDays(-sinceDaysAgo).Date;

        _source = _source.Where(s => !s.Patient.Appointments.Any(a =>
            types.Contains(a.WithType) &&
            a.Status == AppointmentStatus.Submitted &&
            !a.IsNoShow &&
            a.StartDate >= minDate));

        return this;
    }

    public EngagementCriteriaQueryBuilder NoVisits(
        EngagementDate days, AppointmentWithType withType, AppointmentPurpose purpose)
    {
        var (minDate, maxDate) = days.ToDate(_timestamp);

        _source = _source.Where(s => !s.Patient.Appointments.Any(a => 
            a.WithType == withType && 
            a.Purpose == purpose &&
            a.Status == AppointmentStatus.Submitted &&
            !a.IsNoShow &&
            (minDate == null || a.StartDate >= minDate) &&
            (maxDate == null || a.StartDate <= maxDate)));

        return this;
    }

    public EngagementCriteriaQueryBuilder VisitCompleted(params AppointmentWithType[] types)
    {
        _source = _source.Where(s => s.Patient.Appointments
            .Any(a =>
                types.Contains(a.WithType) &&
                !a.IsNoShow &&
                a.Status == AppointmentStatus.Submitted));

        return this;
    }
    
    public EngagementCriteriaQueryBuilder VisitCompleted(int days, params AppointmentWithType[] types)
    {
        var minDate = _timestamp.AddDays(-days).Date;

        _source = _source.Where(s => s.Patient.Appointments
            .Any(a =>
                a.EndDate.Date == minDate &&
                types.Contains(a.WithType) &&
                !a.IsNoShow &&
                a.Status == AppointmentStatus.Submitted));

        return VisitCompleted(types);
    }

    public EngagementCriteriaQueryBuilder VisitCompleted(EngagementDate days, AppointmentWithType[] types)
    {
        var (minDate, maxDate) = days.ToDate(_timestamp);

        _source = _source.Where(s => s.Patient.Appointments
            .Any(a =>
                (minDate == null || a.StartDate.Date >= minDate) &&
                (maxDate == null || a.StartDate.Date <= maxDate) &&
                types.Contains(a.WithType) &&
                !a.IsNoShow &&
                a.Status == AppointmentStatus.Submitted));

        return this;
    }

    public EngagementCriteriaQueryBuilder VisitCompleted(int daysAgo, AppointmentTypes type)
    {
        var minDate = _timestamp.AddDays(-daysAgo).Date;

        _source = _source.Where(s => s.Patient.Appointments
            .Any(a =>
                a.EndDate.Date == minDate &&
                a.Type == type &&
                !a.IsNoShow &&
                a.Status == AppointmentStatus.Submitted));

        return this;
    }
    
    public EngagementCriteriaQueryBuilder NoVisit(AppointmentTypes type)
    {
        _source = _source.Where(s => !s.Patient.Appointments.Any(a =>
            a.Type == type &&
            a.Status == AppointmentStatus.Submitted &&
            !a.IsNoShow));

        return this;
    }

    public EngagementCriteriaQueryBuilder IMCCompleted()
    {
        return VisitCompleted(Provider, HealthCoachAndProvider);
    }
    
    public EngagementCriteriaQueryBuilder IMCCompleted(int daysAgo)
    {
        return VisitCompleted(daysAgo, Provider, HealthCoachAndProvider);
    }
    
    public EngagementCriteriaQueryBuilder MonthsAfterCheckout(int months)
    {
        _source = _source.Where(s => s.Patient.RegistrationDate!.Value.Date.AddMonths(months) < _timestamp.Date);

        return this;
    }

    public EngagementCriteriaQueryBuilder DaysAfterCheckout(int days)
    {
        _source = _source.Where(s => s.Patient.RegistrationDate!.Value.Date.AddDays(days) < _timestamp.Date);

        return this;
    }

    public EngagementCriteriaQueryBuilder DaysAfterCheckout(EngagementDate days)
    {
        var (minDate, maxDate) = days.ToDate(_timestamp);

        _source = _source
            .Where(s => minDate == null || s.Patient.RegistrationDate!.Value.Date >= minDate)
            .Where(s => maxDate == null || s.Patient.RegistrationDate!.Value.Date <= maxDate);

        return this;
    }

    public EngagementCriteriaQueryBuilder DaysSinceLastClarityMessage(int days)
    {
        var minDate = _timestamp.AddDays(-days).Date;

        _source = _source.Where(s =>
            !s.Patient.Conversations
                .Where(cpp => cpp.Conversation.Type != ConversationType.Internal)
                .SelectMany(cpp => cpp.Conversation.ConversationParticipantMessageSentIndexes)
                .Any(msg => msg.LastMessageSentDate.Date >= minDate && msg.Participant.Id != s.Patient.UserId));

        return this;
    }

    public EngagementCriteriaQueryBuilder RenewalIsInLessThan(int months)
    {
        _source = _source.Where(s =>
            s.EndDate.AddMonths(-months).Date < _timestamp.Date);

        return this;
    }

    private EngagementCriteriaQueryBuilder ByPaymentPrice(params PaymentPriceType[] types)
    {
        _source = _source.Where(s => types.Contains(s.PaymentPrice.Type));

        return this;
    }

    public EngagementCriteriaQueryBuilder InsurancePatients()
    {
        return ByPaymentPrice(PaymentPriceType.Insurance, PaymentPriceType.InsurancePromoCode);
    }

    public EngagementCriteriaQueryBuilder CashPatients()
    {
        return ByPaymentPrice(PaymentPriceType.Default, PaymentPriceType.PromoCode);
    }

    public EngagementCriteriaQueryBuilder BirthdayToday()
    {
        _source = _source.Where(s =>
            s.Patient.User.Birthday!.Value.Date.Month == _timestamp.Date.Month &&
            s.Patient.User.Birthday!.Value.Date.Day == _timestamp.Date.Day);

        return this;
    }
}