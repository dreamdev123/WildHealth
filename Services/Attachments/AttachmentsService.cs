using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Common.Models.Documents;
using WildHealth.Common.Models.Employees;
using WildHealth.Application.Services.Employees;

namespace WildHealth.Application.Services.Attachments
{
    public class AttachmentsService : IAttachmentsService
    {
        private readonly IGeneralRepository<Attachment> _attachmentRepository;
        private readonly IAzureBlobService _azureBlobService;
        private readonly IPermissionsGuard _permissionGuard;
        private readonly IEmployeeService _employeeService;

        #region Ctor

        /// <summary>
        /// Creates an instance of AttachmentsService.
        /// </summary>
        /// <param name="attachmentRepository"></param>
        /// <param name="azureBlobService"></param>
        /// <param name="permissionGuard"></param>
        public AttachmentsService(
            IGeneralRepository<Attachment> attachmentRepository,
            IAzureBlobService azureBlobService,
            IPermissionsGuard permissionGuard, 
            IEmployeeService employeeService)
        {
            _attachmentRepository = attachmentRepository;
            _azureBlobService = azureBlobService;
            _permissionGuard = permissionGuard;
            _employeeService = employeeService;
        }

        #endregion

        /// <summary>
        /// <see cref="IAttachmentsService.GetByIdAsync(int)"/>
        /// </summary>
        public async Task<Attachment> GetByIdAsync(int id)
        {
            var attachmentFile = await _attachmentRepository
                .All()
                .ById(id)
                .IncludeLocation()
                .IncludeUser()
                .IncludeNotes()
                .IncludeAgreementConfirmations()
                .IncludePatient()
                .FirstOrDefaultAsync();

            if (attachmentFile is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "File does not exist", exceptionParam);
            }

            return attachmentFile;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetByIdForUserAsync(int)"/>
        /// </summary>
        public async Task<Attachment> GetByIdForUserAsync(int id)
        {
            var attachmentFile = await _attachmentRepository
                .All()
                .IncludeUser()
                .IncludePatient()
                .IncludeNotes()
                .Include(x => x.AgreementConfirmationAttachment)
                .ThenInclude(aca  => aca.AgreementConfirmation)
                .ThenInclude(ac => ac.Patient)
                
                .Include(a => a.NoteContentAttachment)
                .ThenInclude(nca => nca.NoteContent)
                .ThenInclude(nc => nc.Note)
                .ThenInclude(note => note.Patient)
                
                .Include(a => a.NotePdfAttachment)
                .ThenInclude(npa => npa.Note)
                .ThenInclude(note => note.Patient)
                
                .Include(a => a.NotePdfAttachmentWithAmendments)
                .ThenInclude(npwa => npwa.Note)
                .ThenInclude(note=> note.Patient)
                
                .ById(id)
                .FirstOrDefaultAsync();

            if (attachmentFile is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "File does not exist", exceptionParam);
            }

            return attachmentFile;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetByUserIdAsync(int)"/>
        /// </summary>
        /// <param name="userId"></param>
        public async Task<IEnumerable<Attachment>> GetByUserIdAsync(int userId)
        {
            var result = await _attachmentRepository
                .All()
                .IncludeUser()
                .IncludeViewedBy()
                .ByUserId(userId)
                .AvailableForView()
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetByPatientIdAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        public async Task<IEnumerable<Attachment>> GetByPatientIdAsync(int patientId)
        {
            var result = await _attachmentRepository
                .All()
                .IncludePatient()
                .IncludeViewedBy()
                .ByPatientId(patientId)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.ChangeTypeByIdAsync(int, AttachmentType)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        public async Task<Attachment> ChangeTypeByIdAsync(int id, AttachmentType attachmentType)
        {
            var attachment = await GetByIdForUserAsync(id);

            _permissionGuard.AssertPermissions(attachment.UserAttachment.User.Patient);

            attachment.Type = attachmentType;

            _attachmentRepository.Edit(attachment);
            await _attachmentRepository.SaveAsync();

            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetByTypeAttachmentAsync"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="referenceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Attachment>> GetByTypeAttachmentAsync(AttachmentType type, int referenceId)
        {
            var result = await _attachmentRepository
                .All()
                .ByReferenceId(referenceId)
                .ByType(type)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetFileByIdForMessageAsync(int, string)"/>
        /// </summary>
        public async Task<(byte[], string)> GetFileByIdForMessageAsync(int id, string container)
        {
            var record = await GetByIdAsync(id);

            return (await _azureBlobService.GetBlobBytes(container, record.Name), record.Name);
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetFileByPathAsync(string)"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<byte[]> GetFileByPathAsync(string path)
        {
            return await _azureBlobService.GetBlobBytes(path);
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetFileByIdAsync(int, string)"/>
        /// </summary>
        public async Task<(byte[], string)> GetFileByIdAsync(int id, string container)
        {
            var record = await GetByIdForUserAsync(id);

            // Todo : review UserAttachment to get properly hydrated
            if (!(record.UserAttachment?.User?.Patient is null))
            {
                _permissionGuard.AssertPermissions(record.UserAttachment.User.Patient);
            }
           

            return (await _azureBlobService.GetBlobBytes(container, record.Name), record.Name);
        }   

        /// <summary>
        /// <see cref="IAttachmentsService.GetFileByIdUserIdAsync(int, int, string)"/>
        /// </summary>
        public async Task<(byte[], string)> GetFileByIdUserIdAsync(int id, int userId, string container)
        {
            var record = await GetByIdForUserAsync(id);

            if(record.UserAttachment.UserId != userId)
            {
                throw new AppException(HttpStatusCode.Forbidden, $"Access denied.");
            }

            return (await _azureBlobService.GetBlobBytes(container, record.Name), record.Name);
        }

        /// <summary>
        /// Returns file meta data such as Content-type 
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <returns>BlobProperties from file</returns>
        public async Task<BlobProperties> GetFilePropertiesByIdAsync(int id)
        {
            var record = await GetByIdAsync(id);
            return _azureBlobService.GetBlobProperties(record.Description, record.GetOriginName());
        }

        /// <summary>
        /// Return attachment by id for note
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Attachment> GetAttachmentByIdForNotesAsync(int id)
        {
            var result = await _attachmentRepository
                .All()
                .ById(id)
                .FirstAsync();

            return result;
        }

        /// <summary>
        /// Return file by attachment id for note
        /// </summary>
        /// <param name="id"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public async Task<(byte[], string)> GetFileByIdForNotesAsync(int id, string container)
        {
            var record = await GetAttachmentByIdForNotesAsync(id);

            return (await _azureBlobService.GetBlobBytes(container, record.Name), record.Name);
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetFileByIdEncodedAsync(int, string)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public async Task<string?> GetFileByIdEncodedAsync(int id, string container)
        {
            (byte[] fileBytes,_) = await GetFileByIdAsync(id,container);

            if (fileBytes == null)
            {
                return null;
            }
            
            var result = Convert.ToBase64String(fileBytes);
            return new Regex("(?im)A+==+$").Replace(result, "");
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetAsync"/>
        /// </summary>
        public async Task<IEnumerable<Attachment>> GetAsync(Expression<Func<Attachment, bool>> predicate)
        {
            var attachments = await _attachmentRepository
                .Get(predicate)
                .AsNoTracking()
                .ToArrayAsync();

            return attachments;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.CreateOrUpdateWithBlobAsync"/>
        /// </summary>
        public async Task<Attachment> CreateOrUpdateWithBlobAsync(
            string fileName, 
            string description, 
            AttachmentType attachmentType, 
            string path, 
            int referenceId,
            bool isViewableByPatient = true, 
            int blobFileId = 0)
        {
            try
            {
                var existingAttachment = await GetByIdAsync(blobFileId);
                
                existingAttachment.Name = fileName;
                existingAttachment.Type = attachmentType;
                existingAttachment.Description = description;
                existingAttachment.ReferenceId = referenceId;
                existingAttachment.Path = path;
                existingAttachment.IsViewableByPatient = isViewableByPatient;
                
                _attachmentRepository.Edit(existingAttachment);
                
                await _attachmentRepository.SaveAsync();
                
                return existingAttachment;
            }
            catch (AppException e) when(e.StatusCode == HttpStatusCode.NotFound)
            {
                var attachmentFile = new Attachment(attachmentType, fileName, description, path, referenceId, isViewableByPatient);

                await _attachmentRepository.AddAsync(attachmentFile);
                
                await _attachmentRepository.SaveAsync();
                
                return attachmentFile;
            }
        }

        /// <summary>
        /// <see cref="IAttachmentsService.DeleteAsync"/>
        /// </summary>
        public async Task<bool> DeleteAsync(Attachment attachment)
        {
           _attachmentRepository.Delete(attachment);
                
            await _attachmentRepository.SaveAsync();
                
            return true;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.DeleteAttachmentAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAttachmentAsync(int id)
        {
            var attachment = await GetByIdAsync(id);
            
           _attachmentRepository.Delete(attachment);
                
            await _attachmentRepository.SaveAsync();
                
            return true;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.CreateAsync(UserAttachment)"/>
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public async Task<UserAttachment> CreateAsync(UserAttachment attachment)
        {
            await _attachmentRepository.AddRelatedEntity(attachment);

            await _attachmentRepository.SaveAsync();

            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.CreateAsync(AgreementConfirmationAttachment)"/>
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public async Task<AgreementConfirmationAttachment> CreateAsync(AgreementConfirmationAttachment attachment)
        {
            await _attachmentRepository.AddRelatedEntity(attachment);

            await _attachmentRepository.SaveAsync();

            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.CreateAsync(NoteContentAttachment)"/>
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public async Task<NoteContentAttachment> CreateAsync(NoteContentAttachment attachment)
        {
            await _attachmentRepository.AddRelatedEntity(attachment);

            await _attachmentRepository.SaveAsync();

            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetUserAttachmentByTypeAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        public async Task<Attachment?> GetUserAttachmentByTypeAsync(int userId, AttachmentType attachmentType)
        {
            return await _attachmentRepository
                .All()
                .IncludeUser()
                .ByType(attachmentType)
                .ByUserId(userId)
                .FirstOrDefaultAsync();
        }
        
        /// <summary>
        /// <see cref="IAttachmentsService.GetUserAttachmentsByTypeAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        public async Task<Attachment[]> GetUserAttachmentsByTypeAsync(int userId, AttachmentType attachmentType)
        {
            return await _attachmentRepository
                .All()
                .IncludeUser()
                .ByType(attachmentType)
                .ByUserId(userId)
                .ToArrayAsync();
        }
        
        /// <summary>
        /// <see cref="IAttachmentsService.GetUserAttachmentsByTypesAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentTypes"></param>
        /// <returns></returns>
        public async Task<Attachment[]> GetUserAttachmentsByTypesAsync(int userId, AttachmentType[] attachmentTypes)
        {
            return await _attachmentRepository
                .All()
                .IncludeUser()
                .ByTypes(attachmentTypes)
                .ByUserId(userId)
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IAttachmentsService.SetViewedBy"/>
        /// </summary>
        /// <param name="attachmentId"></param>
        /// <param name="userId"></param>
        public async Task SetViewedBy(int attachmentId, int userId)
        {
            var attachmentViewedBy = new AttachmentViewedBy(userId, attachmentId);
            await _attachmentRepository.AddRelatedEntity(attachmentViewedBy);
            await _attachmentRepository.SaveAsync();
        }

        /// <summary>
        /// <see cref="IAttachmentsService.UpdateAsync"/>
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        public async Task<Attachment> UpdateAsync(Attachment attachment)
        {
            _attachmentRepository.Edit(attachment);
            await _attachmentRepository.SaveAsync();
            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.SetViewable"/>
        /// </summary>
        /// <param name="model"></param>
        public async Task<Attachment> SetViewableByPatient(ViewableDocumentModel model)
        {
            var attachment = await GetByIdAsync(model.Id);

            if (attachment is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(model.Id), model.Id);
                throw new AppException(HttpStatusCode.NotFound, "File does not exist", exceptionParam);
            }

            attachment.IsViewableByPatient = model.IsViewableByPatient;
            
           _attachmentRepository.Edit(attachment);
                
            await _attachmentRepository.SaveAsync();
                
            return attachment;
        }

        /// <summary>
        /// <see cref="IAttachmentsService.GetPhotosByEmployeeIds"/>
        /// </summary>
        /// <param name="employeeIds"></param>
        /// <returns></returns>
        public async Task<EmployeeProfilePhotosModel[]> GetPhotosByEmployeeIds(int[] employeeIds)
        {
            
            var employeeProfilePhotos = new List<EmployeeProfilePhotosModel>();
            foreach(var id in employeeIds) {
                
                var employee = await _employeeService.GetEmployeeInfoByIdAsync(id);

                var attachment = await GetUserAttachmentByTypeAsync(employee.UserId, AttachmentType.ProfilePhoto);

                byte[] photo = Array.Empty<byte>();
                if (attachment is not null)
                {
                    photo = await GetFileByPathAsync(attachment.Path);
                }
                
                employeeProfilePhotos.Add(new EmployeeProfilePhotosModel()
                {
                    EmployeeId = id,
                    Photo = photo,
                    PhotoUrl = attachment is null ? String.Empty : await GetSecuredUrl(attachment.GetId(), 5)
                });
            }

            return employeeProfilePhotos.Where(o => o != null).ToArray();
        }
        
        public async Task<EmployeeProfilePhotosModel[]> GetPhotosByEmployeeIdsForDesktop(int[] employeeIds)
        {
            var employees = await _employeeService.GetEmployeesInfoByIdAsync(employeeIds);

            var employeeInfoTasks = employees.Select(async employee =>
            {
                var attachment = employee.User.Attachments.FirstOrDefault(a => a.Attachment.Type == AttachmentType.ProfilePhoto);
                var photoUrl = attachment is not null
                    ? (await _azureBlobService.GetBlobSasUri(attachment.Attachment.Path, null)).ToString()
                    : null;
            
                return new EmployeeProfilePhotosModel
                {
                    EmployeeId = employee.GetId(),
                    PhotoUrl = photoUrl
                };
            }).ToArray();

            return await Task.WhenAll(employeeInfoTasks);
        }
        
        public async Task<string> GetSecuredUrl(int attachmentId, int expirationInMinutes)
        {
            var attachment = await GetByIdAsync(attachmentId);
            
            var response = await _azureBlobService.GetBlobSasUri(attachment.Path, expirationInMinutes);

            return response.ToString();
        }
    }
}
