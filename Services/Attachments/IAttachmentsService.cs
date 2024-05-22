using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Common.Models.Documents;
using WildHealth.Common.Models.Employees;

namespace WildHealth.Application.Services.Attachments
{
    /// <summary>
    /// Provides methods for working with blob files
    /// </summary>
    public interface IAttachmentsService
    {
        /// <summary>
        /// Returns a Attachment record by Id.
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <returns>Attachment record with given Id if exists and null otherwise.</returns>
        Task<Attachment> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns a Attachment record by Id.
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <returns>Attachment record with given Id if exists and null otherwise.</returns>
        Task<Attachment> GetByIdForUserAsync(int id);

        /// <summary>
        /// Returns attachment by full azure path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<byte[]> GetFileByPathAsync(string path);

        /// <summary>
        /// Returns a Attachment by user Id.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns></returns>
        Task<IEnumerable<Attachment>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Returns a Attachment by patient Id.
        /// </summary>
        /// <param name="patientId">Patient Id.</param>
        /// <returns></returns>
        Task<IEnumerable<Attachment>> GetByPatientIdAsync(int patientId);

        /// <summary>
        /// Return attachments by Attachment Type and Reference Id
        /// </summary>
        /// <param name="attachmentType"></param>
        /// <param name="referenceId"></param>
        /// <returns></returns>
        Task<IEnumerable<Attachment>> GetByTypeAttachmentAsync(AttachmentType attachmentType, int referenceId);

        /// <summary>
        /// Returns file data in bytes by Attachment Id
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <param name="container">container.</param>
        /// <returns>Byte representation of image data</returns>
        Task<(byte[], string)> GetFileByIdForMessageAsync(int id, string container);

        /// <summary>
        /// Returns file data in bytes by Attachment Id
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <param name="container">container.</param>
        /// <returns>Byte representation of image data</returns>
        Task<(byte[], string)> GetFileByIdAsync(int id, string container);

        /// <summary>
        /// Return attachment by id for note
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Attachment> GetAttachmentByIdForNotesAsync(int id);

        /// <summary>
        /// Return file by attachment id for note
        /// </summary>
        /// <param name="id"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        Task<(byte[], string)> GetFileByIdForNotesAsync(int id, string container);

        /// <summary>
        /// Returns file data in bytes by Attachment Id and User Id
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="container">container.</param>
        /// <returns>Byte representation of image data</returns>
        Task<(byte[], string)> GetFileByIdUserIdAsync(int id, int userId, string container);

        /// <summary>
        /// Returns file meta data such as Content-type 
        /// </summary>
        /// <param name="id">Attachment Id.</param>
        /// <returns>BlobProperties from file</returns>
        Task<BlobProperties> GetFilePropertiesByIdAsync(int id);

        /// <summary>
        /// Gets file data by Attachment Id as encoded base64 string
        /// </summary>
        /// <param name="id"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        Task<string?> GetFileByIdEncodedAsync(int id, string container);

        /// <summary>
        /// Change attachment type
        /// </summary>ss
        /// <param name="id"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        Task<Attachment> ChangeTypeByIdAsync(int id, AttachmentType attachmentType);

        /// <summary>
        /// Gets Attachment records with filter query.
        /// </summary>
        /// <param name="predicate">An expression which specifies the condition</param>
        /// <returns>List of all Attachment records.</returns>
        Task<IEnumerable<Attachment>> GetAsync(Expression<Func<Attachment, bool>> predicate);

        /// <summary>
        /// Create or update scheduledMessage attachment
        /// </summary>
        /// <param name="attachmentName"></param>
        /// <param name="description"></param>
        /// <param name="attachmentType"></param>
        /// <param name="path"></param>
        /// <param name="referenceId"></param>
        /// <param name="isViewableByPatient"></param>
        /// <param name="attachmentId"></param>
        /// <returns></returns>
        Task<Attachment> CreateOrUpdateWithBlobAsync(string attachmentName, string description, AttachmentType attachmentType, string path, int referenceId, bool isViewableByPatient = true, int attachmentId = 0);

        /// <summary>
        /// Deletes a record
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(Attachment attachment);

        /// <summary>
        /// Deletes a scheduledMessage attachment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> DeleteAttachmentAsync(int id);

        /// <summary>
        /// Creates user attachment
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        Task<UserAttachment> CreateAsync(UserAttachment attachment);

        /// <summary>
        /// Creates agreement confirmation attachment
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        Task<AgreementConfirmationAttachment> CreateAsync(AgreementConfirmationAttachment attachment);
        
        /// <summary>
        /// Creates note content attachment
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        Task<NoteContentAttachment> CreateAsync(NoteContentAttachment attachment);

        /// <summary>
        /// Returns user attachment by attachment type
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        Task<Attachment?> GetUserAttachmentByTypeAsync(int userId, AttachmentType attachmentType);
        
        /// <summary>
        /// Returns user attachments by attachment type
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentType"></param>
        /// <returns></returns>
        Task<Attachment[]> GetUserAttachmentsByTypeAsync(int userId, AttachmentType attachmentType);
        
        /// <summary>
        /// Returns user attachments by attachment types
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="attachmentTypes"></param>
        /// <returns></returns>
        Task<Attachment[]> GetUserAttachmentsByTypesAsync(int userId, AttachmentType[] attachmentTypes);

        /// <summary>
        /// Update the attachment as viewed by the specified user
        /// </summary>
        /// <param name="attachmentId"></param>
        /// <param name="userId"></param>
        Task SetViewedBy(int attachmentId, int userId);

        /// <summary>
        /// Update attachment
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        Task<Attachment> UpdateAsync(Attachment attachment);

        /// <summary>
        /// Update the attachment as viewable by the specified user
        /// </summary>
        /// <param name="model"></param>
        Task<Attachment> SetViewableByPatient(ViewableDocumentModel model);

        /// <summary>
        /// Returns attachments by EmployeeIds
        /// </summary>
        /// <param name="employeeIds"></param>
        Task<EmployeeProfilePhotosModel[]> GetPhotosByEmployeeIds(int[] employeeIds);
        
        /// <summary>
        /// Returns attachments by EmployeeIds for Desktop
        /// </summary>
        Task<EmployeeProfilePhotosModel[]> GetPhotosByEmployeeIdsForDesktop(int[] employeeIds);

        /// <summary>
        /// Returns a safe url that will last for the given attachmentId assuming the attachment was stored with a valid container in the description
        /// </summary>
        /// <param name="attachmentId"></param>
        /// <param name="expirationInMinutes"></param>
        /// <returns></returns>
        Task<string> GetSecuredUrl(int attachmentId, int expirationInMinutes);
    }
}