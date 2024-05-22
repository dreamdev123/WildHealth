using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Inputs;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class DownloadFileInputCommand : IRequest<(FileInput fileInput, byte[] content)>, IValidatabe
    {
        public int Id { get; }
        
        public int PatientId { get; }
        
        public DownloadFileInputCommand(int id, int patientId)
        {
            Id = id;
            PatientId = patientId;
        }

        #region validation

        private class Validator : AbstractValidator<DownloadFileInputCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
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