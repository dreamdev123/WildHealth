using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using WildHealth.Common.Models.Users;
using WildHealth.Common.Validators;
using WildHealth.Domain.Enums.User;
using FluentValidation;
using MediatR;
using WildHealth.Common.Models.Agreements;

namespace WildHealth.Application.Commands.Patients;

public class RegisterPreauthorizePatientCommand : IRequest<CreatedPatientModel>, IValidatabe
{
    public string FirstName { get; }
    public string LastName { get; }
    public Gender Gender { get; }
    public string Email { get; }
    public DateTime Birthday { get; }
    public string PhoneNumber { get; }
    public string Password { get; }
    public AddressModel BillingAddress { get; }
    public AddressModel ShippingAddress { get; }
    public int PracticeId { get; }
    public string PreauthorizeRequestToken { get; }
    public ConfirmAgreementModel[] Agreements { get; }
    public int[] AddOnIds { get;  } = Array.Empty<int>();
    
    public RegisterPreauthorizePatientCommand(
        string firstName, 
        string lastName, 
        Gender gender, 
        string email, 
        DateTime birthday, 
        string phoneNumber, 
        string password, 
        AddressModel billingAddress, 
        AddressModel shippingAddress,
        int practiceId, 
        string preauthorizeRequestToken,
        ConfirmAgreementModel[] agreements,
        int[] addOnIds)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        Email = email;
        Birthday = birthday;
        PhoneNumber = phoneNumber;
        Password = password;
        BillingAddress = billingAddress;
        ShippingAddress = shippingAddress;
        PracticeId = practiceId;
        PreauthorizeRequestToken = preauthorizeRequestToken;
        Agreements = agreements;
        AddOnIds = addOnIds;
    }
    
    #region validation

    private class Validator : AbstractValidator<RegisterPreauthorizePatientCommand>
    {
        public Validator()
        {
            RuleFor(x => x.BillingAddress).NotNull().SetValidator(new AddressModel.Validator());
            RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new AddressModel.Validator());
            RuleFor(x => x.FirstName).NotNull().NotEmpty();
            RuleFor(x => x.LastName).NotNull().NotEmpty();
            RuleFor(x => x.Gender).NotEqual(Gender.None);
            RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
            RuleFor(x => x.Birthday).NotEmpty();
            RuleFor(x => x.PhoneNumber).NotNull().NotEmpty();
            RuleFor(x => x.Password).SetValidator(new PasswordValidator());
            RuleFor(x => x.PreauthorizeRequestToken).NotNull().NotEmpty();
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleForEach(x => x.AddOnIds).GreaterThan(0);
        }
    }

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}