using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class SynchronizeDorothyRecordsCommand : IRequest, IValidatabe
{
    public int NumberOfRecordsToSynchronize { get; }
    
    public int ShardSize { get; }
    
    public int PracticeId { get; }
    
    public bool SkipSync { get; }
    
    public SynchronizeDorothyRecordsCommand(int numberOfRecordsToSynchronize, int shardSize, int practiceId, bool skipSync = false)
    {
        NumberOfRecordsToSynchronize = numberOfRecordsToSynchronize;
        ShardSize = shardSize;
        PracticeId = practiceId;
        SkipSync = skipSync;
    }
    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<SynchronizeDorothyRecordsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NumberOfRecordsToSynchronize).GreaterThan(0);
            RuleFor(x => x.ShardSize).GreaterThan(0);
            RuleFor(x => x.PracticeId).GreaterThan(0);
        }
    }

    #endregion
}