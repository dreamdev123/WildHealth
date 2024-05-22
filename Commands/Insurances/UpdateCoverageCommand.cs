using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Entities.Insurances;
using FluentValidation;
using MediatR;
using WildHealth.Common.Models.Common;

namespace WildHealth.Application.Commands.Insurances;

public class UpdateCoverageCommand : IRequest<Coverage>, IValidatabe
{
    public int Id { get; }
    
    public int? UserId { get; }
    
    public int? PatientId { get; }

    public int InsuranceId { get; }

    public string MemberId { get; }
    
    public bool IsPrimary { get; }
        
    public AttachmentModel[]? Attachments { get; }
    
    public PolicyHolderModel? PolicyHolder { get; }

    protected UpdateCoverageCommand(
        int id,
        int? userId,
        int? patientId,
        int insuranceId,
        string memberId,
        bool isPrimary,
        AttachmentModel[]? attachments,
        PolicyHolderModel? policyHolder)
    {
        Id = id;
        UserId = userId;
        PatientId = patientId;
        InsuranceId = insuranceId;
        MemberId = memberId;
        IsPrimary = isPrimary;
        Attachments = attachments;
        PolicyHolder = policyHolder;
    }
    
    public static UpdateCoverageCommand ByUser(
        int id,
        int userId,
        int insuranceId,
        string memberId,
        bool isPrimary,
        AttachmentModel[]? attachments,
        PolicyHolderModel? policyHolder)
    {
        return new UpdateCoverageCommand(
            id: id,
            userId: userId,
            patientId: null,
            insuranceId: insuranceId,
            memberId: memberId,
            isPrimary: isPrimary,
            attachments: attachments,
            policyHolder: policyHolder
        );
    }
    
    public static UpdateCoverageCommand OnBehalf(
        int id,
        int patientId,
        int insuranceId,
        string memberId,
        bool isPrimary,
        AttachmentModel[]? attachments,
        PolicyHolderModel? policyHolder)
    {
        return new UpdateCoverageCommand(
            id: id,
            userId: null,
            patientId: patientId,
            insuranceId: insuranceId,
            memberId: memberId,
            isPrimary: isPrimary,
            attachments: attachments,
            policyHolder: policyHolder
        );
    }

    #region validation

    private class Validator : AbstractValidator<UpdateCoverageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
            RuleFor(x => x.PatientId).GreaterThan(0).When(x => x.PatientId.HasValue);
            RuleFor(x => x.InsuranceId).GreaterThan(0);
            RuleFor(x => x.MemberId).NotNull().NotEmpty();
            RuleFor(x => x.PolicyHolder).SetValidator(new PolicyHolderModel.Validator());
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