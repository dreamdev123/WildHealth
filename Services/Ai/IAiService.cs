using System.Threading.Tasks;
using WildHealth.ClarityCore.Models.Ai;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Services.Ai
{
    public interface IAiService
    {
        /// <summary>
        /// Returns patient knowledge base
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<KnowledgeBaseResponseModel> GetPatientKnowledgeBase(string patientId, ReportType reportType);

        /// <summary>
        /// Generates embedding from text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        Task<EmbeddingResponseModel> GenerateEmbedding(string text, int coreEmbeddingModelId);
    }
}
