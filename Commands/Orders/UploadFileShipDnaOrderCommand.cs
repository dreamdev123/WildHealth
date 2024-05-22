using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;


namespace WildHealth.Application.Commands.Orders
{
    public class UploadFileShipDnaOrderCommand : IRequest, IValidatabe
    {
        public IFormFile File { get; }
        
        public UploadFileShipDnaOrderCommand(
            IFormFile file)
        {
            File = file;
        }
        
        #region validation
        
        private class Validator: AbstractValidator<UploadFileShipDnaOrderCommand>
        {
            public Validator()
            {
                RuleFor(x => x.File).NotNull();
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