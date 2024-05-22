using MediatR;
using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Google.Maps.Models;

namespace WildHealth.Application.Commands.Maps;


public class GeocodeAddressCommand : IRequest<GoogleGeocodeResponse>, IValidatabe
{
    public string Address { get; }

    public GeocodeAddressCommand(
        string address)
    {
        Address = address;
    }

    #region private

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GeocodeAddressCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Address)
                .NotNull()
                .NotEmpty();
        }
    }
    #endregion
}
