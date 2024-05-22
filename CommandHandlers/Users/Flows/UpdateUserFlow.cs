using System;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Users.Flows;

class UpdateUserFlow : IMaterialisableFlow
{
    private readonly User _user;
    private readonly string _firstName;
    private readonly string _lastName;
    private readonly string _phoneNumber;
    private readonly DateTime? _birthday;
    private readonly Gender _gender;
    private readonly Address _billingAddress;
    private readonly Address _shippingAddress;
    private readonly UserIdentity _userIdentity;
    private readonly UserType? _userType;
    private readonly bool _emailExists;
    private readonly string _email;
    private readonly bool? _registrationCompleted;

    public UpdateUserFlow(
        User user, 
        string firstName, 
        string lastName, 
        string phoneNumber, 
        DateTime? birthday, 
        Gender gender,
        Address billingAddress, 
        Address shippingAddress,
        UserIdentity userIdentity,
        UserType? userType,
        bool emailExists,
        string email,
        bool? registrationCompleted = null)
    {
        _user = user;
        _firstName = firstName;
        _lastName = lastName;
        _phoneNumber = phoneNumber;
        _birthday = birthday;
        _gender = gender;
        _billingAddress = billingAddress;
        _shippingAddress = shippingAddress;
        _userIdentity = userIdentity;
        _userType = userType;
        _emailExists = emailExists;
        _email = email;
        _registrationCompleted = registrationCompleted;
    }

    public MaterialisableFlowResult Execute()
    {
        _user.FirstName = _firstName.FirstCharToUpper();
        _user.LastName = _lastName.FirstCharToUpper();
        _user.PhoneNumber = _phoneNumber;
        _user.Birthday = _birthday;
        _user.Gender = _gender;
        _user.BillingAddress = _billingAddress;
        _user.ShippingAddress = _shippingAddress;

        if (_registrationCompleted ?? false)
        {
            _user.CompleteRegistration();
        }
        
        AssertIdentityExist();
        UpdateUserType();
        UpdateEmail();
        return _user.Updated() + _userIdentity.Updated();
    }
    
    private void AssertIdentityExist()
    {
        if (_userIdentity is null)
        {
            throw new AppException(HttpStatusCode.NotFound, "User does not exist");
        }
    }

    private void UpdateUserType()
    {
        if (_userType.HasValue)
        {
            _userIdentity.Type = _userType.Value;
        }
    }
    private void UpdateEmail()
    {
        if (!string.IsNullOrWhiteSpace(_email) && !string.Equals(_email, _userIdentity.Email, StringComparison.CurrentCultureIgnoreCase))
        {
            if (_emailExists)
            {
                throw new AppException(HttpStatusCode.BadRequest, "User name " + _email + " is already taken");
            }
            _userIdentity.Email = _email;
            _userIdentity.User.Email = _email;
        }
    }
}