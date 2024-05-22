using System;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Domain.Enums.Sms;

namespace WildHealth.Application.Commands.Sms;

public record RevokeSmsConsentCommand(
    Guid PhoneUserIdentity, 
    string RecipientPhoneNumber, 
    string SenderPhoneNumber, 
    string MessagingServiceSid, 
    string IntegrationEventJson) : IRequest<SmsConsent>, IValidatabe
{
    public bool IsValid()
    {
        return new Validator().Validate(this).IsValid;
    }

    public void Validate()
    {
    }

    private class Validator : AbstractValidator<RevokeSmsConsentCommand>
    {
        public Validator()
        {
        }
    }
}