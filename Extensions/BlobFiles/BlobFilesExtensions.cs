using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Inputs;

namespace WildHealth.Application.Extensions.BlobFiles
{
    public static class BlobFilesExtensions
    {
        public static void Update(this BlobFile origin, BlobFile source)
        {
            origin.Name = source.Name;
            origin.MediaType = source.MediaType;
            origin.ContainerName = source.ContainerName;
        }

        public static string DeterminateContentType(this string fileName)
        {
            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);
            return contentType!;
        }

        public static async Task<byte[]> GetBytes(this IFormFile file)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return stream.ToArray();
        }
        
        public static string GenerateStorageFileName(this IFormFile file, DateTime date)
        {
            string[] fileName = file.FileName.Split('.');

            var extension = file.FileName.Substring(file.FileName.LastIndexOf(".", StringComparison.Ordinal));
            
            return $"{fileName[0].ToString().Replace(' ', '_')}_{date.ToEpochTime()}{extension}";
        }
    
        public static string GenerateStorageFileName(this IFormFile file, int userId, AttachmentType attachmentType)
        {
            var fileName = GetSafeName(file.FileName);

            return $"Users/{userId}/{attachmentType.ToString()}/{Guid.NewGuid().ToString()}/{fileName}";
        }

        public static string GenerateStorageFileName(this IFormFile file, FileInputType type, int patientId, DateTime date)
        {
            var extension = file.FileName.Substring(file.FileName.LastIndexOf(".", StringComparison.Ordinal));
        
            return $"{patientId}_{type.ToString()}_{date.ToEpochTime()}{extension}";
        }
    
        public static string GenerateStorageFileName(this IFormFile file, int noteId, bool withAmendments)
        {
            var attachmentType = withAmendments
                ? AttachmentType.NotePdfWithAmendments
                : AttachmentType.NotePdf;
            return $"Notes/{noteId}/{attachmentType.ToString()}/{file.FileName}";
        }
    
        public static string GenerateStorageFileName(this IFormFile file, int insuranceId, int practiceId)
        {
            var extension = file.FileName.Substring(file.FileName.LastIndexOf(".", StringComparison.Ordinal));
            
            return $"{insuranceId}_{practiceId}_payer_logo{extension}";
        }
        
        public static string GenerateStorageFileName(this IFormFile file, SourceType sourceType)
        {
            var fileName = GetSafeName(file.FileName);

            return string.Empty.GenerateStorageFileName(fileName, sourceType);
        }
        
        public static string GenerateStorageFileName(this string text, string fileName, SourceType sourceType)
        {
            fileName = GetSafeName(fileName);
            
            return $"{sourceType}-{Guid.NewGuid().ToString()}-{fileName}";
        }
    
        private static string GetSafeName(string val)
        {
            return val.Replace('/', '-').Replace(' ', '-').Replace(':', '-').Replace('#', '-');
        }
    }
}