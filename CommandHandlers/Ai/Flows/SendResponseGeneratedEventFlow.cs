using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Ai;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Ai;
using WildHealth.Domain.Constants;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.AiAnalytics;
using WildHealth.IntegrationEvents.AiAnalytics.Payloads;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Ai.Flows;

public class SendResponseGeneratedEventFlow : IMaterialisableFlow
{
    private readonly AiAnalyticsLoggingModel _loggingModel;
    private readonly IAiAnalyticsLoggingParser _parser;
    public SendResponseGeneratedEventFlow(
        AiAnalyticsLoggingModel loggingModel,
        IAiAnalyticsLoggingParser parser)
    {
        _loggingModel = loggingModel;
        _parser = parser;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_loggingModel.AnalyticsLogType != AiConstants.AnalyticsLogTypes.Generated)
        {
            var message = $"{_loggingModel.AnalyticsLogType} does not match expected AnalyticsLogType of {AiConstants.AnalyticsLogTypes.Generated}";
            throw new AppException(HttpStatusCode.InternalServerError, message);
        }
        return GetIntegrationEvents().ToFlowResult();
    }

    private IEnumerable<BaseIntegrationEvent> GetIntegrationEvents()
    {
        var generatedResponseReturnedAtString = _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.SentAt);
        var generatedResponseReturnedAt = DateTime.UtcNow;
        DateTime.TryParse(generatedResponseReturnedAtString, out generatedResponseReturnedAt);

        yield return new AiAnalyticsIntegrationEvent(
            payload: new AiResponseGeneratedPayload(
                assistanceRequestId: _loggingModel.AssistanceRequestId,
                knowledgeBaseVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.KnowledgeBaseVersion),
                embeddingModelName: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.EmbeddingModelName),
                embeddingModelVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.EmbeddingModelVersion),
                similarityVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.SimilarityVersion),
                promptBrewerVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.PromptBrewerVersion),
                promptSentContent: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.PromptSentContent),
                responseGeneratorModelName: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.ResponseGeneratorModelName),
                responseGeneratorModelVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.ResponseGeneratorModelVersion),
                generatedResponseContent: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.GeneratodResponseContent),
                generatedResponseReturnedAt: generatedResponseReturnedAt,
                isTest: _parser.GetValueOrDefaultForKey<bool>(_loggingModel, AiConstants.AiGeneratedConstants.IsTest),
                intermediateStepsFile: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.IntermediateStepsFile),
                appReleaseVersion: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiGeneratedConstants.AppReleaseVersion)
            ),
            user: new UserMetadataModel(_loggingModel.UserUniversalId), 
            eventDate: DateTime.UtcNow);
    }
}