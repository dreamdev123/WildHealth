using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Orders;

public class CreateReferralOrderCommand : IRequest<ReferralOrder>, IValidatabe
{
    public int PatientId { get; }
    
    public int? EmployeeId { get; }
    
    public CreateReferralOrderItemModel[] Items { get; }
    
    public ReferralOrderDataModel Data { get; }
    
    public bool SendForReview { get; }
    
    public bool IsCompleted { get; }
    
    public CreateReferralOrderCommand(
        int patientId, 
        int? employeeId,
        CreateReferralOrderItemModel[] items, 
        ReferralOrderDataModel data, 
        bool sendForReview, bool isCompleted)
    {
        PatientId = patientId;
        EmployeeId = employeeId;
        Items = items;
        Data = data;
        SendForReview = sendForReview;
        IsCompleted = isCompleted;
    }

    #region private

    private class Validator : AbstractValidator<CreateReferralOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);

            RuleFor(x => x.Items)
                .NotNull()
                .NotEmpty()
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleForEach(x => x.Items)
                .SetValidator(new CreateReferralOrderItemModel.Validator())
                .When(x => x.IsCompleted || x.SendForReview);
            
            RuleFor(x => x.Data.PatientName)
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
            
            RuleFor(x => x.EmployeeId)
                .NotNull()
                .GreaterThan(0)
                .When(x => x.SendForReview || x.IsCompleted);
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