using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Inputs;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Inputs;
using MediatR;
using WildHealth.Inputs.Services.InputsParser;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class UploadInputsFileCommandHandler : IRequestHandler<UploadInputsFileCommand, FileInput>
    {
        private const string Container = AzureBlobContainers.PatientsInputs;
        private readonly IInputsService _inputsService;
        private readonly IBlobFilesService _blobFilesService;
        private readonly ILogger _logger;
        private readonly IInputsParser _inputsParser;

        public UploadInputsFileCommandHandler(
            IInputsService inputsService, 
            IBlobFilesService blobFilesService,
            ILogger<UploadInputsFileCommandHandler> logger,
            IInputsParser inputsParser)
        {
            _inputsService = inputsService;
            _blobFilesService = blobFilesService;
            _logger = logger;
            _inputsParser = inputsParser;
        }

        public async Task<FileInput> Handle(UploadInputsFileCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Uploading of file input for patient with id: {command.PatientId} has been started.");
            
            var inputType = command.Type;
            var dataProvider = command.DataProvider;
            var patientId = command.PatientId;
            var bytes = command.Bytes;
            var fileName = command.FileName;

            var aggregator = await _inputsService.GetAggregatorAsync(patientId, inputType);
            
            var blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(
                fileBytes: bytes,
                fileName: fileName,
                containerName: Container
            );
            
            
            var date = _inputsParser.ParseObservationDate(inputType, dataProvider, bytes);

            var input = new FileInput(aggregator, blobFile, inputType, dataProvider, date);
                
            await _inputsService.CreateFileInputAsync(input);

            _logger.LogInformation($"Uploading of file input for patient with id: {command.PatientId} has been finished.");

            return input;
        }
    }
}