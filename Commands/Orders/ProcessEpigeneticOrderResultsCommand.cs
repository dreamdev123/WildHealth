using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders;

public class ProcessEpigeneticOrderResultsCommand : IRequest, IValidatabe
{
    public int PatientId { get; }
    
    public int OrderId { get; }

    public string OrderNumber { get; }
    
    public ProcessEpigeneticOrderResultsCommand(
        int patientId, 
        int orderId, 
        string orderNumber)
    {
        PatientId = patientId;
        OrderId = orderId;
        OrderNumber = orderNumber;
    }
    
    #region private

    private class Validator : AbstractValidator<ProcessEpigeneticOrderResultsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.OrderId).GreaterThan(0);
            RuleFor(x => x.OrderNumber).NotNull().NotEmpty();
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