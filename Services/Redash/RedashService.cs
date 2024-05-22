using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;
using WildHealth.Redash.Clients.Credentials;
using WildHealth.Redash.Clients.Models;
using WildHealth.Redash.Clients.WebClients;

namespace WildHealth.Application.Services.Redash;

public class RedashService : IRedashService
{
    private readonly IRedashQueryWebClient _queryWebClient;
    private readonly IOptions<RedashOptions> _options;

    public RedashService(
        IRedashQueryWebClient queryWebClient,
        IOptions<RedashOptions> options)
    {
        _queryWebClient = queryWebClient;
        _options = options;
    }

    public async Task<QueryResultModel<FhirChargeSubmissionModel>> QueryFhirChargeSubmissionsAsync(DateTime startDate, DateTime endDate, int practiceId)
    {
        var url = _options.Value.Url;
        var apiKey = _options.Value.ApiKey;
        
        _queryWebClient.Initialize(new CredentialsModel(apiKey, url));
        
        var queryId = "839";
        
        var parameters = new Dictionary<string, object> {
            {"start_date", $"{startDate.ToUniversalTime().ToString("O")}"},
            {"end_date", $"{endDate.ToUniversalTime().ToString("O")}"},
            {"practice_id", practiceId.ToString()},
        };
        
        return await _queryWebClient.GetQueryResults<FhirChargeSubmissionModel>(queryId, parameters);
    }
    
    public async Task<QueryResultModel<FhirChargeDenialModel>> QueryFhirChargeDenialsAsync(DateTime startDate, DateTime endDate, int practiceId)
    {
        var url = _options.Value.Url;
        var apiKey = _options.Value.ApiKey;
        
        _queryWebClient.Initialize(new CredentialsModel(apiKey, url));
        
        var queryId = "838";
        
        var parameters = new Dictionary<string, object> {
            {"start_date", $"{startDate.ToUniversalTime().ToString("O")}"},
            {"end_date", $"{endDate.ToUniversalTime().ToString("O")}"},
            {"practice_id", practiceId.ToString()},
        };

        return await _queryWebClient.GetQueryResults<FhirChargeDenialModel>(queryId, parameters);
    }


    /// <summary>
    /// Gets the NPS score information
    /// </summary>
    /// <param name="npsScoreType"></param>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public async Task<QueryResultModel<PatientNpsScoreModel>> QueryNpsScoresAsync(
        int patientId)
    {
        var url = _options.Value.Url;
        var apiKey = _options.Value.ApiKey;
        
        _queryWebClient.Initialize(new CredentialsModel(apiKey, url));
        
        var queryId = "1146";
        
        var parameters = new Dictionary<string, object> {
            {"patient_id", patientId.ToString()},
        };

        try
        {
            return await _queryWebClient.GetQueryResults<PatientNpsScoreModel>(queryId, parameters);
        }
        catch
        {
            return new QueryResultModel<PatientNpsScoreModel>
            {
                Data = new DataModel<PatientNpsScoreModel>
                {
                    Rows = Array.Empty<PatientNpsScoreModel>()
                }
            };
        }
    }
}