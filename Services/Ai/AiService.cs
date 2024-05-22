using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.ClarityCore.Exceptions;
using WildHealth.ClarityCore.Models.Ai;
using WildHealth.ClarityCore.WebClients.Ai;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.Ai
{
    public class AiService : IAiService
    {
        private readonly IAiWebClient _aiWebClient;
        private readonly ILogger _logger;

        public AiService(
            IAiWebClient aiWebClient,
            ILogger<AiService> logger)
        {
            _aiWebClient = aiWebClient;
            _logger = logger;
        }

        public async Task<KnowledgeBaseResponseModel> GetPatientKnowledgeBase(string patientId, ReportType reportType)
        {
            return await _aiWebClient.GetPatientKnowledgeBase(patientId, reportType);
        }

        public async Task<EmbeddingResponseModel> GenerateEmbedding(string text, int coreEmbeddingModelId)
        {
            return await _aiWebClient.GenerateEmbedding(text, coreEmbeddingModelId);
        }
    }
}
