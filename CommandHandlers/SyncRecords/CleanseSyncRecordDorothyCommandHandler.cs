using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WildHealth.Application.Commands.Maps;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Attributes;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Google.Maps.Models;
using WildHealth.Lob.Clients.Models;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class CleanseSyncRecordDorothyCommandHandler : IRequestHandler<CleanseSyncRecordDorothyCommand>
{
    private readonly int ZIP_CODE_LENGTH = 5;
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly ILogger<CleanseSyncRecordDorothyCommandHandler> _logger;
    private readonly IMediator _mediator;

    public CleanseSyncRecordDorothyCommandHandler(
        ISyncRecordsService syncRecordsService,
        ILogger<CleanseSyncRecordDorothyCommandHandler> logger,
        IMediator mediator)
    {
        _syncRecordsService = syncRecordsService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(CleanseSyncRecordDorothyCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cleansing of dorothy sync records has: started");

        var statusesToCleanse = new[] { SyncRecordStatus.PendingCleansing };
        var records = await _syncRecordsService.GetByTypeAndStatus<SyncRecordDorothy>(
            SyncRecordType.Dorothy,
            statusesToCleanse, 
            command.NumberOfRecordsToCleanse);

        foreach (var record in records)
        {
            try
            {
                CleanseWhiteSpace(record);
                CleanseName(record);
                await CleanseAddress(record);

                await UpdateSyncRecord(record);
            }
            catch (Exception)
            {
                _logger.LogInformation($"Failed to cleanse dorothy record with id = {record.GetId()}");
            }
            
        }
        
        _logger.LogInformation("Cleansing of dorothy sync records has: finished");
    }

    #region private

    private async Task CleanseAddress(SyncRecordDorothy record)
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////
        // Want to run everything through this geocoding.  If we receive a response we can assume
        // it is good and perform the update, if there's a null response, we keep what we had
        ////////////////////////////////////////////////////////////////////////////////////////////////
        var response = await VerifyAddress(record);

        record.StreetAddress1 = response?.PrimaryLine ?? record.StreetAddress1;
        record.StreetAddress2 = response?.SecondaryLine ?? record.StreetAddress2;
        record.City =  response?.Components.City ?? record.City;
        record.State = response?.Components.State ?? record.State;
        record.ZipCode = response?.Components.ZipCode ?? record.ZipCode;
        
        ////////////////////////////////////////////////////////////////////////////////////////////////
        // Add some padding to the ZipCodes if they're not blank/null and have a length less
        // than the expected length of 5
        ////////////////////////////////////////////////////////////////////////////////////////////////
        if (!string.IsNullOrEmpty(record.ZipCode) && record.ZipCode.Length < ZIP_CODE_LENGTH)
        {
            var prefixLen = ZIP_CODE_LENGTH - record.ZipCode.Length;
            var prefix = string.Join("", new int[prefixLen]);

            record.ZipCode = $"{prefix}{record.ZipCode}";
        }
    }

    private void CleanseWhiteSpace(SyncRecordDorothy record)
    {
        foreach (var property in typeof(SyncRecordDorothy).GetProperties().Where(prop => prop.IsDefined(typeof(SyncRecordProperty), false)))
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }
            
            if (property.Name == nameof(SyncRecordDorothy.PolicyId))
            {
                var policyId = property.GetValue(record)?.ToString();
                property.SetValue(record, !string.IsNullOrEmpty(policyId) ? string.Concat(policyId.Where(c => !char.IsWhiteSpace(c))) : null); 
            }
            else
            {
                property.SetValue(record, property.GetValue(record)?.ToString()?.Trim());
            }
        }
    }

    private void CleanseName(SyncRecordDorothy record)
    {
        record.FirstName = RemoveDiacritics(record.FirstName);
        record.LastName = RemoveDiacritics(record.LastName);
    }

    private async Task UpdateSyncRecord(SyncRecordDorothy record)
    {
        record.SetStatus(SyncRecordStatus.PendingValidation);
        await _syncRecordsService.UpdateAsync(record);
    }

    private async Task<LobVerifyAddressResponseModel?> VerifyAddress(SyncRecordDorothy record)
    {
        try
        {
            return await _mediator.Send(new VerifyAddressCommand(fullAddress: GetAddressFromRecord(record)));
        }
        catch
        {
            return null;
        }
    }

    private string GetAddressFromRecord(SyncRecordDorothy record)
    {
        return !string.IsNullOrEmpty(record.FullAddress) 
            ? record.FullAddress
            : string.Join(" ", new[] {record.StreetAddress1, record.StreetAddress2, record.City, record.State, record.ZipCode});
    }

    // https://stackoverflow.com/a/249126
    private string RemoveDiacritics(string text) 
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    #endregion
}