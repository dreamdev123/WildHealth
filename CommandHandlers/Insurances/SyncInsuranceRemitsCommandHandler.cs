using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.IntegrationEvents.Fhir;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using WildHealth.Waystar.Clients.Models.Era;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class SyncInsuranceRemitsCommandHandler : IRequestHandler<SyncInsuranceRemitsCommand>
{
    private readonly IClearinghouseIntegrationServiceFactory _clearinghouseIntegrationServiceFactory;
    private readonly IRemitFilesService _remitFilesService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SyncInsuranceRemitsCommandHandler> _logger;

    public SyncInsuranceRemitsCommandHandler(
        IClearinghouseIntegrationServiceFactory clearinghouseIntegrationServiceFactory,
        IRemitFilesService remitFilesService,
        IEventBus eventBus,
        ILogger<SyncInsuranceRemitsCommandHandler> logger)
    {
        _clearinghouseIntegrationServiceFactory = clearinghouseIntegrationServiceFactory;
        _remitFilesService = remitFilesService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(SyncInsuranceRemitsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Syncing of remit files for practice id = {command.PracticeId} has: started");
        
        var clearinghouseService = await _clearinghouseIntegrationServiceFactory.CreateAsync(command.PracticeId);

        var remitFiles = await clearinghouseService.GetRemitFilesAsync(command.PracticeId);
        
        foreach (var file in remitFiles)
        {
            try
            {
                var existingRemitFile = await _remitFilesService.GetByFileNameAsync(file.FileName);

                if (existingRemitFile is null)
                {

                    var remits = await GetRemitsFromFile(
                        fileName: file.FileName,
                        practiceId: command.PracticeId,
                        clearinghouseService: clearinghouseService);

                    var remitFile = new RemitFile(
                        fileName: file.FileName,
                        payer: file.Payer,
                        addedDate: file.AddedDate,
                        practiceId: command.PracticeId,
                        remits: remits);

                    remitFile = await _remitFilesService.CreateAsync(remitFile);

                    await PublishRemitFileProcessedEvent(remitFile);
                    
                    _logger.LogInformation($"Syncing of remit files for practice id = {command.PracticeId} has: processed file {file.FileName}");
                }
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Syncing of remit files for practice id = {command.PracticeId} has: failed to process file {file.FileName} with error {e.Message}");
            }
        }
        
        _logger.LogInformation($"Syncing of remit files for practice id = {command.PracticeId} has: finished");
    }

    #region private

    private int ResolveClaimId(int claimId)
    {
        var manuallyEnteredClaims = new Dictionary<int, int>()
        {
            { 341969, 5715 },
            { 341970, 5716 },
            { 341971, 5717 },
            { 341975, 5718 },
            { 341976, 5719 },
            { 341979, 5720 },
            { 341980, 5721 },
            { 341984, 5722 },
            { 341987, 5723 }
        };

        return manuallyEnteredClaims.ContainsKey(claimId) ? manuallyEnteredClaims[claimId] : claimId;
    }

    private async Task<Remit[]> GetRemitsFromFile(string fileName, int practiceId, IClearinghouseIntegrationService clearinghouseService)
    {
        var remitModels =
            await clearinghouseService.GetRemitsFromFileAsync(fileName, practiceId);

        return remitModels.Select(model => new Remit(
            claimId: ResolveClaimId(Convert.ToInt32(model.ClaimId)),
            patientFirstName: model.PatientFirstName,
            patientLastName: model.PatientLastName,
            payerName: model.PayerName,
            paymentAmount: model.TotalPaymentAmount,
            patientIdentificationQualifier: model.PatientIdentificationQualifier,
            patientIdentifier: model.PatientIdentifier,
            remitServicePayments: model.ServicePayments.Select(svc => new RemitServicePayment(
                serviceCode: svc.ServiceCode,
                chargeAmount: svc.ChargeAmount,
                paymentAmount: svc.PaymentAmount,
                serviceFrom: svc.ServiceFrom,
                serviceTo: svc.ServiceTo,
                remitAdjustments: svc.Adjustments.Select(adj => new RemitAdjustment(
                    groupCode: adj.GroupCode,
                    reasonCode: adj.ReasonCode,
                    amount: adj.Amount,
                    quantity: adj.Quantity)).ToArray()
            )).ToArray()
        )).ToArray();
    }

    private async Task PublishRemitFileProcessedEvent(RemitFile file)
    {
        var payload = new FhirRemitFileProcessedPayload(
            id: file.GetId().ToString(),
            practiceId: file.PracticeId,
            fileName:file.FileName,
            payer: file.Payer,
            addedDate: file.AddedDate);
        
        await _eventBus.Publish(new FhirRemitIntegrationEvent(
            payload: payload,
            eventDate: DateTime.UtcNow));
    }

    #endregion
}