using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.CommandHandlers.Employees.Flows;

public class UpdateFellowFlow : IMaterialisableFlow
{
    private readonly Fellow _fellow;
    private readonly string _firstName;
    private readonly string _lastName;
    private readonly string _email;
    private readonly string _phoneNumber;
    private readonly string _credentials;

    public UpdateFellowFlow(
        Fellow fellow,
        string firstName, 
        string lastName, 
        string email, 
        string phoneNumber, 
        string credentials)
    {
        _fellow = fellow;
        _firstName = firstName;
        _lastName = lastName;
        _email = email;
        _phoneNumber = phoneNumber;
        _credentials = credentials;
    }

    public MaterialisableFlowResult Execute()
    {
        _fellow.FirstName = _firstName;
        _fellow.LastName = _lastName;
        _fellow.Email = _email;
        _fellow.PhoneNumber = _phoneNumber;
        _fellow.Credentials = _credentials;

        return _fellow.Updated();
    }
}