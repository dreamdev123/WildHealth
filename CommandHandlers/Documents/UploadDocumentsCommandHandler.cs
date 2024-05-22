using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Events.Attachments;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Extensions;
using System.Linq;
using WildHealth.Application.Extensions.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Entities.Files.Blobs;

namespace WildHealth.Application.CommandHandlers.Documents
{
    public class UploadDocumentsCommandHandler : IRequestHandler<UploadDocumentsCommand, List<Attachment>>
    { 
        private const int DocumentSourceTypeIdPatientDocument = 8;
        private const string Container = AzureBlobContainers.Attachments;
        private readonly IAttachmentsService _attachmentsService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IPatientsService _patientsService;
        private readonly IMediator _mediator;
        private readonly IDocumentSourceTypesService _documentSourceTypesService;
        private readonly MaterializeFlow _materialize;

        public UploadDocumentsCommandHandler(
            IAttachmentsService attachmentsService,
            IAzureBlobService azureBlobService,
            IPatientsService patientsService, 
            IMediator mediator,
            IDocumentSourceTypesService documentSourceTypesService,
            MaterializeFlow materialize)
        {
            _attachmentsService = attachmentsService;
            _azureBlobService = azureBlobService;
            _patientsService = patientsService;
            _mediator = mediator;
            _documentSourceTypesService = documentSourceTypesService;
            _materialize = materialize;
        }

        public async Task<List<Attachment>> Handle(UploadDocumentsCommand command, CancellationToken cancellationToken)
        {
            var attachments = new List<Attachment>();

            var patient = await _patientsService
               .GetByIdAsync(command.PatientId, PatientSpecifications.PatientUserSpecification);

            var userId = patient.User.GetId();
            var attachmentType = command.AttachmentType;

            foreach (var (file, index) in command.Documents.WithIndex())
            {
                var bytes = await file.GetBytes();

                var fileName = file.GenerateStorageFileName(userId, attachmentType);

                var pathFile = await _azureBlobService.CreateUpdateBlobBytes(Container, fileName, bytes);
                var isViewable = command.IsViewableByPatientFileIndexes is null || command.IsViewableByPatientFileIndexes.Contains(index);

                var attachment = await _attachmentsService.CreateOrUpdateWithBlobAsync(
                    attachmentName: fileName,
                    description: $"File from user with id {userId} in category {attachmentType.ToString()}",
                    attachmentType: attachmentType,
                    path: pathFile,
                    referenceId: userId,
                    isViewableByPatient: isViewable
                );

                var userAttachment = new UserAttachment(patient.User, attachment);

                await _attachmentsService.CreateAsync(userAttachment);

                attachments.Add(attachment);
                
                if (command.IsSendToKb) {
                    var documentSourceType = await _documentSourceTypesService.GetByIdAsync(DocumentSourceTypeIdPatientDocument);
                    var recommendedTags = Enumerable.Empty<HealthCategoryTag>().ToArray();

                    var blobFile = new BlobFile
                    {
                        Name = fileName,
                        ContainerName = Container,
                        MediaType = fileName.DeterminateContentType(),
                        Uri = pathFile
                    };

                    await new AddDocumentSourceFlow(
                        name: $"Patient Document ({attachment.GetId()})", 
                        documentSourceType: documentSourceType, 
                        personaIds: null, 
                        recommendedTags: recommendedTags,
                        tags: null, 
                        file: blobFile, 
                        url: null,
                        patientId: command.PatientId).Materialize(_materialize);
                }
            }

            await _mediator.Publish(new DocumentsUploadedEvent(patient, attachments.Count, command.UploadedByUserId), cancellationToken);

            return attachments;
        }
    }
}
