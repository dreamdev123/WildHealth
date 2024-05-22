using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using WildHealth.Application.Commands.AddOns;
using WildHealth.Application.Services.AddOns;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.AddOns;

public class UpdateAddOnsFromFileCommandHandler : IRequestHandler<UpdateAddOnsFromFileCommand>
{
    private readonly ILogger _logger;
    private readonly IAddOnsService _addOnsService;

    public UpdateAddOnsFromFileCommandHandler(IAddOnsService addOnsService,
        ILogger<UpdateAddOnsFromFileCommandHandler> logger)
    {
        _addOnsService = addOnsService;
        _logger = logger;
    }

    public async Task Handle(UpdateAddOnsFromFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting reading addOns from file.");
        var newAddOns = await GetAddOnsFromFileAsync(request.AddOnsFile, request.Provider);
        
        if (!newAddOns.Any())
        {
            throw new AppException(HttpStatusCode.BadRequest, "File is empty.");
        }
        
        _logger.LogInformation("Reading addOns from file has finished. Starting updating the addOns in the DB");
        var existingAddOns = await _addOnsService.GetByProviderAsync(request.Provider);
        foreach (var newAddOn in newAddOns)
        {
            if (existingAddOns.ContainsKey(newAddOn.IntegrationId))
            {
                var existingAddon = existingAddOns[newAddOn.IntegrationId];
                existingAddon.Update(newAddOn);
                await _addOnsService.UpdateAsync(existingAddon);
            }
            else
            {
                await _addOnsService.CreateAddOnAsync(newAddOn);
            }
        }
        _logger.LogInformation("Updating addOns in the DB has finished.");
    }

    private async Task<List<AddOn>> GetAddOnsFromFileAsync(IFormFile file, AddOnProvider provider)
    {
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var excelPackage = new ExcelPackage(stream);
        var sheet = excelPackage.Workbook.Worksheets.First();
        var addOns = new List<AddOn>();
        var row = GetHeaderRowNumber(sheet);
        while(true)
        {
            row++;

            var serviceCode = sheet.Cells[row, 1].GetValue<string>();

            if (String.IsNullOrEmpty(serviceCode)) break; //when we reach empty codes we stop the cycle
            
            // if (!int.TryParse(serviceCode, out _)) break; //when we reach non-numeric codes we stop the cycle

            var serviceName = sheet.Cells[row, 2].GetValue<string>();
            var priceString = sheet.Cells[row, 3].GetValue<string>();
            var price = decimal.Parse(priceString, NumberStyles.Currency);

            var addOn = new AddOn
            {
                Name = serviceName,
                Description = serviceName,
                IntegrationId = serviceCode,
                Price = price,
                Provider = provider,
                IsActive = true,
                CanOrder = true
            };
            addOns.Add(addOn);
        }

        return addOns;
    }

    private int GetHeaderRowNumber(ExcelWorksheet sheet)
    {
        //looking for a header in first 20 rows
        for (var row = 1; row <= 20; row++)
        {
            if (string.Compare(sheet.Cells[row, 1].GetValue<string>(), "service code", StringComparison.OrdinalIgnoreCase) == 0
                && string.Compare(sheet.Cells[row, 2].GetValue<string>(), "service name", StringComparison.OrdinalIgnoreCase) == 0
                && string.Compare(sheet.Cells[row, 3].GetValue<string>(), "test price", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return row;
            }
        }
        throw new AppException(HttpStatusCode.BadRequest, "Incorrect file format");
    }
}