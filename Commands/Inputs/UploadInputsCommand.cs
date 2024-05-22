using FluentValidation;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class UploadInputsCommand : IRequest<FileInput>, IValidatabe
    {
        public FileInputType Type { get; }
        
        public FileInputDataProvider DataProvider { get; }
                
        public IFormFile File { get; }
        
        public int PatientId { get; }
        
        public UploadInputsCommand(
            FileInputType type, 
            FileInputDataProvider dataProvider, 
            IFormFile file, 
            int patientId)
        {
            Type = type;
            DataProvider = dataProvider;
            File = file;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<UploadInputsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.DataProvider).IsInEnum();
                RuleFor(x => x.File).NotNull();
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
        /// <returns></returns>
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}