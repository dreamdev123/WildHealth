using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.CommandHandlers.Challenges.Flows;
using WildHealth.Application.Commands.Challenges;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Challenges;
using WildHealth.Common.Constants;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Challenges;

public class CreateChallengeCommandHandler : IRequestHandler<CreateChallengeCommand, Unit>
{
    private readonly IChallengesService _challengesService;
    private readonly IAzureBlobService _azureBlobService;
    private readonly MaterializeFlow _materializer;

    public CreateChallengeCommandHandler(
        IChallengesService challengesService, 
        IAzureBlobService azureBlobService, 
        MaterializeFlow materializer)
    {
        _challengesService = challengesService;
        _azureBlobService = azureBlobService;
        _materializer = materializer;
    }

    public async Task<Unit> Handle(CreateChallengeCommand request, CancellationToken cancellationToken)
    {
        var lastChallenge = await _challengesService.GetLastChallengeInQueue().ToOption();
        var imageUniqueName = UniqueFileName(request.Image);
        await _azureBlobService.CreateUpdateBlobBytes(AzureBlobContainers.Media, imageUniqueName, await request.Image.GetBytes());

        await new CreateChallengeFlow(
            lastChallenge, 
            imageUniqueName, 
            request.Title, 
            request.Description, 
            request.DurationInDays, 
            DateTime.UtcNow).Materialize(_materializer);
            
        return Unit.Value;
    }

    private string UniqueFileName(IFormFile file) => $"challenges/{Guid.NewGuid().ToString()[..8]}{file.FileName}";
}