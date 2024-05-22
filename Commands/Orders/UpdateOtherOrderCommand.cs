using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders;

public class UpdateOtherOrderCommand : IRequest<OtherOrder>, IValidatabe
{
    public int Id { get; }
    
    public int EmployeeId { get; }
    
    public CreateOtherOrderItemModel[] Items { get; }
    
    public OtherOrderDataModel Data { get; }
    
    public bool SendForReview { get; }
    
    public bool IsCompleted { get; }
    
    public UpdateOtherOrderCommand(
        int id, 
        int employeeId, 
        CreateOtherOrderItemModel[] items, 
        OtherOrderDataModel data, 
        bool sendForReview, bool isCompleted)
    {
        Id = id;
        EmployeeId = employeeId;
        Items = items;
        Data = data;
        SendForReview = sendForReview;
        IsCompleted = isCompleted;
    }

    #region private

    private class Validator : AbstractValidator<UpdateOtherOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0)
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Items)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleForEach(x => x.Items)
                .SetValidator(new CreateOtherOrderItemModel.Validator())
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.PatientName)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.PatientAddress)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.OrderingProvider)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.InsuranceProvider)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.InsuranceId)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
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