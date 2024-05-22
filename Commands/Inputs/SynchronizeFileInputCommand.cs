using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Inputs;
using Microsoft.AspNetCore.Http;
using WildHealth.Domain.Enums.Inputs;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class SynchronizeFileInputCommand : IRequest<FileInput>, IValidatabe
    {
        public FileInputType Type { get; }
        
        public FileInputDataProvider DataProvider { get; }
                
        public IFormFile File { get; }
        
        public string BlobUri { get; }

        public string ContainerName { get; }
        
        public int PatientId { get; }
        
        public SynchronizeFileInputCommand(
            FileInputType type, 
            FileInputDataProvider dataProvider, 
            IFormFile file, 
            string blobUri, 
            string containerName, 
            int patientId)
        {
            Type = type;
            DataProvider = dataProvider;
            File = file;
            BlobUri = blobUri;
            ContainerName = containerName;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<SynchronizeFileInputCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.DataProvider).IsInEnum();
                RuleFor(x => x.File).NotNull();
                RuleFor(x => x.BlobUri).NotNull().NotEmpty();
                RuleFor(x => x.ContainerName).NotNull().NotEmpty();
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }

        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}