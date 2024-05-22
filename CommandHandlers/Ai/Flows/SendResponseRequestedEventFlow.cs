using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Ai;
using WildHealth.Common.Models.Ai;
using WildHealth.Domain.Constants;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.AiAnalytics;
using WildHealth.IntegrationEvents.AiAnalytics.Payloads;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Constants;

namespace WildHealth.Application.CommandHandlers.Ai.Flows;

public class SendResponseRequestedEventFlow : IMaterialisableFlow
{
    private readonly AiAnalyticsLoggingModel _loggingModel;
    private readonly IAiAnalyticsLoggingParser _parser;
    public SendResponseRequestedEventFlow(
        AiAnalyticsLoggingModel loggingModel,
        IAiAnalyticsLoggingParser parser)
    {
        _loggingModel = loggingModel;
        _parser = parser;
    }

    public MaterialisableFlowResult Execute()
    {
        if (_loggingModel.AnalyticsLogType != AiConstants.AnalyticsLogTypes.Requested)
        {
            var message = $"{_loggingModel.AnalyticsLogType} does not match expected AnalyticsLogType of {AiConstants.AnalyticsLogTypes.Requested}";
            throw new AppException(HttpStatusCode.InternalServerError, message);
        }
        return GetIntegrationEvents().ToFlowResult();
    }

    private IEnumerable<BaseIntegrationEvent> GetIntegrationEvents()
    {
        var requestSentAtString = _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.SentAt);
        var requestSentAt = DateTime.UtcNow;
        DateTime.TryParse(requestSentAtString, out requestSentAt);

        yield return new AiAnalyticsIntegrationEvent(
            payload: new AiResponseRequestedPayload(
                assistanceRequestId: _loggingModel.AssistanceRequestId,
                conversationId: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.ConversationId),
                messageId: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.MessageId),
                requestVendor: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.Vendor),
                flowType: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.FlowType),
                requesterUniversalId: _loggingModel.UserUniversalId,
                requesterQueryContent: _parser.GetValueOrDefaultForKey<string>(_loggingModel, AiConstants.AiRequestConstants.RequestorQueryContent),
                requestSentAt: requestSentAt,
                isTest: _parser.GetValueOrDefaultForKey<bool>(_loggingModel, AiConstants.AiRequestConstants.IsTest)
            ),
            user: new UserMetadataModel(_loggingModel.UserUniversalId), 
            eventDate: DateTime.UtcNow);
    }
}