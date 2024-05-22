using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.CommandHandlers.Employees.Flows;

public class CreateFellowFlow : IMaterialisableFlow
{
    private readonly Roster _roster;
    private readonly string _firstName;
    private readonly string _lastName;
    private readonly string _email;
    private readonly string _phoneNumber;
    private readonly string _credentials;

    public CreateFellowFlow(
        Roster roster, 
        string firstName, 
        string lastName, 
        string email, 
        string phoneNumber, 
        string credentials)
    {
        _roster = roster;
        _firstName = firstName;
        _lastName = lastName;
        _email = email;
        _phoneNumber = phoneNumber;
        _credentials = credentials;
    }

    public MaterialisableFlowResult Execute()
    {
        var fellow = new Fellow(_roster)
        {
            FirstName = _firstName,
            LastName = _lastName,
            Email = _email,
            PhoneNumber = _phoneNumber,
            Credentials = _credentials
        };

        return fellow.Added();
    }
}