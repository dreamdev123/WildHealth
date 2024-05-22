using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.InsuranceConfigurations;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.CommandHandlers.InsuranceConfigurations;

public class UploadInsuranceLogoCommandHandler : IRequestHandler<UploadInsuranceLogoCommand, string>
{
    private const string Container = InsuranceCdnContainers.PayerLogos;
    private readonly IInsuranceBlobService _insuranceBlobService;
    private readonly IInsuranceConfigsService _insuranceConfigsService;
    private readonly InsuranceBlobOptions _options;
    private readonly ILogger<UploadInsuranceLogoCommandHandler> _logger;

    public UploadInsuranceLogoCommandHandler(
        IInsuranceBlobService insuranceBlobService,
        IInsuranceConfigsService insuranceConfigsService,
        IOptions<InsuranceBlobOptions> options,
        ILogger<UploadInsuranceLogoCommandHandler> logger)
    {
        _insuranceBlobService = insuranceBlobService;
        _insuranceConfigsService = insuranceConfigsService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> Handle(UploadInsuranceLogoCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Uploading of an insurance logo for insuranceConfiguration id = {command.InsuranceConfigurationId} has: started");

        var file = command.File;

        ValidateExtension(file);

        var insuranceConfiguration = await _insuranceConfigsService.GetByIdAsync(command.InsuranceConfigurationId);
        
        var bytes = await file.GetBytes();
        var fileName = file.GenerateStorageFileName(
            insuranceId: insuranceConfiguration.InsuranceId,
            practiceId: insuranceConfiguration.PracticeId);

        await _insuranceBlobService.CreateUpdateBlobBytes(
            containerName: Container,
            blobName: fileName,
            fileBytes: bytes);

        var cdnUrl = GenerateCdnUrl(fileName);
            
        insuranceConfiguration.PayerLogo = cdnUrl;

        await _insuranceConfigsService.UpdateAsync(insuranceConfiguration);

        return cdnUrl;
    }

    private void ValidateExtension(IFormFile file)
    {
        var ext = System.IO.Path.GetExtension(file.FileName.ToLower());
        var allowedTypes = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
        };
        
        if (!allowedTypes.Contains(ext))
        {
            throw new DomainException($"That filetype is not allowed.  Please use one of {String.Join(",", allowedTypes)}");
        }
    }

    #region private

    private string GenerateCdnUrl(string fileName)
    {
        var container = Container;
        
        if (container.Contains("{0}"))
        {
            container = string.Format(container, _options.Environment);
        }
        
        return $"{_options.CdnUrl}/{container}/{fileName}";
    }

    #endregion
}