using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Models.Common;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Attachments;
using System.Collections.Generic;
using WildHealth.Application.Services.Patients;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using MediatR;
using System;

namespace WildHealth.Application.CommandHandlers.Insurances
{
    public class UploadInsuranceCommandHandler : IRequestHandler<UploadInsuranceCommand, Attachment[]>
    {
        private readonly IAttachmentsService _attachmentsService;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IPatientsService _patientsService;
        private readonly IUsersService _usersService;
        private readonly ILogger _logger;
        private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;

        public UploadInsuranceCommandHandler(
            IAttachmentsService attachmentsService,
            IAzureBlobService azureBlobService,
            IPatientsService patientsService,
            IUsersService usersService, 
            ILogger<UploadInsuranceCommandHandler> logger,
            IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory)
        {
            _attachmentsService = attachmentsService;
            _azureBlobService = azureBlobService;
            _patientsService = patientsService;
            _usersService = usersService;
            _logger = logger;
            _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        }

        public async Task<Attachment[]> Handle(UploadInsuranceCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Insurance uploading for user with [Id] = {command.UserId} started.");
            
            var user = await GetUserAsync(command);

            var attachments = new List<Attachment>();
            
            await AssertCanUploadInsuranceAsync(user);

            foreach (var attachment in command.Attachments)
            {
                attachments.Add(await UploadAttachmentAsync(user, attachment, command.CoverageId));
            }
            
            _logger.LogInformation($"Insurance uploading for user with [Id] = {command.UserId} started.");

            return attachments.ToArray();
        }
        
        #region private

        /// <summary>
        /// Fetches and returns user 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<User> GetUserAsync(UploadInsuranceCommand command)
        {
            if (command.UserId.HasValue)
            {
                var specification = UserSpecifications.UserWithIntegrations;

                return await _usersService.GetAsync(command.UserId.Value, specification);
            }

            if (command.PatientId.HasValue)
            {
                var specification = PatientSpecifications.PatientWithIntegrations;

                var patient = await _patientsService.GetByIdAsync(command.PatientId.Value, specification);

                return patient.User;
            }

            throw new AppException(HttpStatusCode.BadRequest, "Patient is should be Greater Than than 0");
        }
        
        /// <summary>
        /// Asserts if user can upload insurance
        /// </summary>
        private async Task AssertCanUploadInsuranceAsync(User user)
        {
            if (user.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer) is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Can't upload insurance.");
            }

            var patientId = user.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer).Value;

            try
            {
                var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);
                
                var patient = await pmService.GetPatientAsync(
                    id: patientId,
                    practiceId: user.PracticeId
                );
                
                if (patient is null)
                {
                    throw new AppException(HttpStatusCode.BadRequest, "Can't upload insurance.");
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Upload insurance has failed with [Error]: {e}");
                throw new AppException(HttpStatusCode.BadRequest, "Can't upload insurance.");
            }
        }

        /// <summary>
        /// Uploads attachment
        /// </summary>
        /// <param name="user"></param>
        /// <param name="attachment"></param>
        /// <param name="coverageId"></param>
        private async Task<Attachment> UploadAttachmentAsync(User user, AttachmentModel attachment, string coverageId)
        {
            var blobName = $"users/{user.GetId()}/{attachment.Name}";
            var bytes = await GetBytesAsync(attachment.File);

            var path = await _azureBlobService.CreateUpdateBlobBytes(
                containerName: AzureBlobContainers.Attachments,
                blobName: blobName,
                fileBytes: bytes
            );

            var userAttachment = new UserAttachment(
                type: attachment.Type,
                name: attachment.Name,
                description: coverageId,
                path: path,
                user: user
            );

            await _attachmentsService.CreateAsync(userAttachment);
            
            return userAttachment.Attachment;
        }
        
        /// <summary>
        /// Returns file bytes
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static async Task<byte[]> GetBytesAsync(IFormFile file)
        {
            await using var stream = new MemoryStream();
            
            await file.CopyToAsync(stream);
            
            return stream.ToArray();
        }
        
        #endregion
    }
}