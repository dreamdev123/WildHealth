using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Application.Utils.Timezones;

namespace WildHealth.Application.Commands.Notifications;

public class SendSmsNotificationCommand : IRequest, IValidatabe
{
    public string Text { get; }
    public DateTime? SignupDateFrom { get; }
    public DateTime? SignupDateTo { get; }
    public string[] TextParameters { get; }
    public string[] PaymentPlans { get; }
    public int[] OnlyPatientIds { get; }
    public bool? HasCompletedAppointment { get; }
    public bool? HasActiveSubscription { get; }
    public DateTime? SendAt { get; }

    public SendSmsNotificationCommand(
        string text, 
        DateTime? signupDateFrom, 
        DateTime? signupDateTo, 
        string[] textParameters, 
        string[] paymentPlans, 
        int[] onlyPatientIds, 
        bool? hasCompletedAppointment, 
        bool? hasActiveSubscription, 
        DateTime? sendAt)
    {
        Text = text;
        SignupDateFrom = signupDateFrom;
        SignupDateTo = signupDateTo;
        TextParameters = textParameters;
        PaymentPlans = paymentPlans;
        OnlyPatientIds = onlyPatientIds;
        HasCompletedAppointment = hasCompletedAppointment;
        HasActiveSubscription = hasActiveSubscription;
        SendAt = sendAt;
    }

    #region validation

    private class Validator : AbstractValidator<SendSmsNotificationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Text).NotEmpty();
            RuleFor(x => x.SignupDateTo)
                .GreaterThan(x => x.SignupDateFrom)
                .When(x => x.SignupDateFrom.HasValue && x.SignupDateTo.HasValue);
            RuleFor(x => x.SendAt)
                .GreaterThan(TimezoneHelper.GetCurrentLocalTime("Eastern Standard Time"));

        }
    }
    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);
    
    #endregion
}