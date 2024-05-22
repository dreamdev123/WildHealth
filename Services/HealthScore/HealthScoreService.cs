using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.ClarityCore.Exceptions;
using WildHealth.ClarityCore.Models.HealthScore;
using WildHealth.ClarityCore.WebClients.HealthScore;
using WildHealth.Domain.Enums.Orders;

namespace WildHealth.Application.Services.HealthScore
{
    public class HealthScoreService : IHealthScoreService
    {
        private readonly IHealthScoreWebClient _healthScoreWebClient;
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly ILabOrdersService _labOrdersService;
        private readonly ILogger _logger;

        public HealthScoreService(
            IHealthScoreWebClient healthScoreWebClient,
            IDnaOrdersService dnaOrdersService,
            ILabOrdersService labOrdersService,
            ILogger<HealthScoreService> logger)
        {
            _healthScoreWebClient = healthScoreWebClient;
            _dnaOrdersService = dnaOrdersService;
            _labOrdersService = labOrdersService;
            _logger = logger;
        }
        
        /// <summary>
        /// <see cref="IHealthScoreService.IsHealthScoreAvailableAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<bool> IsHealthScoreAvailableAsync(int patientId)
        {
            var dnaOrders = (await _dnaOrdersService.GetAsync(patientId)).ToArray();

            var result = dnaOrders.Any(x => x.Status == OrderStatus.Completed);

            var labOrders = (await _labOrdersService.GetPatientOrdersAsync(patientId)).ToArray();

            if (labOrders.Any() && result)
            {
                result = labOrders.Any(x => x.Status == OrderStatus.Completed);
            }

            return result;
        }

        public async Task<HealthScoreResponseModel> GetPatientHealthScore(string patientId)
        {
            try
            {
                return await _healthScoreWebClient.GetPatientHealthScore(patientId);
            }
            catch (ClarityCoreException ex)
            {
                _logger.LogError($"Error during fetching health scores for patient with [Id] = {patientId}", ex);
                return EmptyScore(patientId);
            }
        }

        public async Task<HealthScoreResponseModel> RunPatientHealthScore(string patientId)
        {
            try
            {
                return await _healthScoreWebClient.RunPatientHealthScore(patientId);
            }
            catch (ClarityCoreException ex)
            {
                _logger.LogError($"Error during running health scores for patient with [Id] = {patientId}", ex);
                return EmptyScore(patientId);
            }
        }

        #region private

        private static HealthScoreResponseModel EmptyScore(string patientId) => new ()
        {
            PatientScore = new PatientScoreResponseModel
            {
                Patient = int.Parse(patientId),
                ExternalPatientId = patientId
            },
            Categories = new List<CategoryResponseModel>(),
            Terms = new List<TermsResponseModel>()
        };

        #endregion
    }
}
