using System;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Extensions;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Users.Flows;

public class CreateOrUpdateUserIdentityFlow : IMaterialisableFlow
{
    private readonly byte[] _passwordHash;
    private readonly byte[] _passwordSalt;
    private readonly string _email;
    private readonly UserType _userType;
    private readonly bool _isVerified;
    private readonly string? _firstName;
    private readonly string? _lastName;
    private readonly int _practiceId;
    private readonly string? _phoneNumber;
    private readonly DateTime? _birthDate;
    private readonly Gender _gender;
    private readonly AddressModel? _billingAddress;
    private readonly AddressModel? _shippingAddress;
    private readonly bool _isRegistrationCompleted;
    private readonly string? _note;
    private readonly bool _marketingSms;
    private readonly bool _meetingRecordingConsent;
    private readonly UserIdentity _userIdentity;
    private readonly bool _emailIsInUse;
    
    public CreateOrUpdateUserIdentityFlow(
        byte[] passwordHash, 
        byte[] passwordSalt, 
        string email, 
        UserType userType, 
        bool isVerified, 
        string? firstName, 
        string? lastName, 
        int practiceId, 
        string? phoneNumber, 
        DateTime? birthDate, 
        Gender gender, 
        AddressModel? billingAddress, 
        AddressModel? shippingAddress, 
        bool isRegistrationCompleted, 
        string? note, 
        bool marketingSms,
        bool meetingRecordingConsent,
        UserIdentity userIdentity,
        bool emailIsInUse)
    {
        _passwordHash = passwordHash;
        _passwordSalt = passwordSalt;
        _email = email;
        _userType = userType;
        _isVerified = isVerified;
        _firstName = firstName;
        _lastName = lastName;
        _practiceId = practiceId;
        _phoneNumber = phoneNumber;
        _birthDate = birthDate;
        _gender = gender;
        _billingAddress = billingAddress;
        _shippingAddress = shippingAddress;
        _isRegistrationCompleted = isRegistrationCompleted;
        _note = note;
        _marketingSms = marketingSms;
        _meetingRecordingConsent = meetingRecordingConsent;
        _userIdentity = userIdentity;
        _emailIsInUse = emailIsInUse;
    }
    public MaterialisableFlowResult Execute()
    {
        if (_emailIsInUse)
            throw new AppException(HttpStatusCode.BadRequest, "Email address is already registered.");
        
        if (_userIdentity != null)
            return UpdateUserIdentity();
        
        return CreateUserIdentity();
    }

    private EntityAction UpdateUserIdentity()
    {
        _userIdentity.PasswordHash = _passwordHash;
        _userIdentity.PasswordSalt = _passwordSalt;
        _userIdentity.IsVerified = _isVerified;
        _userIdentity.User.Options = new UserOptions { MarketingSMS = _marketingSms };

        return _userIdentity.Updated();
    }

    private EntityAction CreateUserIdentity()
    {
        var identity = new UserIdentity(_practiceId)
        {
            Email = _email,
            PasswordHash = _passwordHash,
            PasswordSalt = _passwordSalt,
            Type = _userType,
            IsVerified = _isVerified,
            User = new User(_practiceId)
            {
                Email = _email,
                FirstName = string.IsNullOrEmpty(_firstName) ? string.Empty : _firstName.FirstCharToUpper(),
                LastName = string.IsNullOrEmpty(_lastName) ? string.Empty : _lastName.FirstCharToUpper(),
                PhoneNumber = _phoneNumber,
                Birthday = _birthDate,
                Gender = _gender,
                BillingAddress = SetAddress(_billingAddress),
                ShippingAddress = SetAddress(_shippingAddress),
                IsRegistrationCompleted = _isRegistrationCompleted,
                Note = _note,
                Options = new UserOptions
                {
                    MarketingSMS = _marketingSms, 
                    MeetingRecordingConsent = _meetingRecordingConsent
                }
            }
        };
        return identity.Added();
    }

    private Address SetAddress(AddressModel? model)
    {
        return model is null
            ? new Address()
            : new Address
            {
                Country = model.Country,
                City = model.City,
                State = model.State,
                ZipCode = model.ZipCode,
                StreetAddress1 = model.StreetAddress1,
                StreetAddress2 = model.StreetAddress2
            };
    }
}