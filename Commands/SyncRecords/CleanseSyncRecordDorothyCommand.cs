using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class CleanseSyncRecordDorothyCommand : IRequest, IValidatabe
{
    public int NumberOfRecordsToCleanse { get; }

    public CleanseSyncRecordDorothyCommand(int numberOfRecordsToCleanse)
    {
        NumberOfRecordsToCleanse = numberOfRecordsToCleanse;
    }
    
    #region validation

    private class Validator : AbstractValidator<CleanseSyncRecordDorothyCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NumberOfRecordsToCleanse).GreaterThan(0);
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