using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Ai;

namespace WildHealth.Application.Services.Ai;

public interface IAiResourceService
{
    Task<AiResourceModel> GetAiResourceAsync(Guid universalId, string resourceType);
}