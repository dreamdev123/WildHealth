using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class UploadInputsFileCommand : IRequest<FileInput>, IValidatabe
    {
        public FileInputType Type { get; }
        
        public FileInputDataProvider DataProvider { get; }
                
        public byte[] Bytes { get; }
        
        public string FileName { get; }
        
        public int PatientId { get; }
        
        public UploadInputsFileCommand(
            FileInputType type, 
            FileInputDataProvider dataProvider, 
            byte[] bytes, 
            string fileName,
            int patientId)
        {
            Type = type;
            DataProvider = dataProvider;
            Bytes = bytes;
            FileName = fileName;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<UploadInputsFileCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.DataProvider).IsInEnum();
                RuleFor(x => x.Bytes).NotNull().NotEmpty();
                RuleFor(x => x.FileName).NotNull().NotEmpty();
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