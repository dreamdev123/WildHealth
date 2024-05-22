using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models._Base;
using WildHealth.Jenny.Clients.Models;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Documents;

public class ProcessDocumentChunksCommand : IRequest, IValidatable
{
    public int? DocumentSourceId { get; }
    
    public string RequestId { get; }
    
    public DocumentChunkResponseModel Chunks { get; }
    
    private ProcessDocumentChunksCommand(
        int? documentSourceId,
        string requestId,
        DocumentChunkResponseModel chunks)
    {
        DocumentSourceId = documentSourceId;
        RequestId = requestId;
        Chunks = chunks;
    }

    public static ProcessDocumentChunksCommand ByDocumentId(
        int documentSourceId,
        DocumentChunkResponseModel chunks)
    {
        return new ProcessDocumentChunksCommand(
            documentSourceId: documentSourceId,
            requestId: string.Empty,
            chunks: chunks
        );
    }
    
    public static ProcessDocumentChunksCommand ByRequestId(
        string requestId,
        DocumentChunkResponseModel chunks)
    {
        return new ProcessDocumentChunksCommand(
            documentSourceId: null,
            requestId: requestId,
            chunks: chunks
        );
    }

    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<ProcessDocumentChunksCommand>
    {
        public Validator()
        {
            RuleFor(x => x.DocumentSourceId)
                .GreaterThan(0)
                .When(x => x.DocumentSourceId.HasValue);

            RuleFor(x => x.RequestId)
                .NotEmpty()
                .When(x => x.DocumentSourceId is null);
            
            RuleFor(x => x.Chunks).NotNull();
        }
    }

    #endregion
}