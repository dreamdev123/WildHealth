using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Patients;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.PharmacyInfo;

public class CreateOrUpdatePharmacyInfoCommand : IRequest<PatientPharmacyInfo>, IValidatabe
{
    public int PatientId { get; }
    
    public string Name { get; }
    
    public string Phone { get; }
    
    public string Address { get; }

    public string City { get; }
    
    public string ZipCode { get; }
    
    public string State { get; }
    
    public string Country { get; }
    
    public CreateOrUpdatePharmacyInfoCommand(
        int patientId, 
        string name, 
        string phone, 
        string address, 
        string city, 
        string zipCode, 
        string state, 
        string country)
    {
        PatientId = patientId;
        Name = name;
        Phone = phone;
        Address = address;
        City = city;
        ZipCode = zipCode;
        State = state;
        Country = country;
    }
    
    #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<CreateOrUpdatePharmacyInfoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.Phone).NotNull().NotEmpty();
            RuleFor(x => x.Address).NotNull().NotEmpty();
            RuleFor(x => x.City).NotNull().NotEmpty();
            RuleFor(x => x.ZipCode).NotNull().NotEmpty();
            RuleFor(x => x.State).NotNull().NotEmpty();
            RuleFor(x => x.Country).NotNull().NotEmpty();
        }
    }
    
    #endregion
}