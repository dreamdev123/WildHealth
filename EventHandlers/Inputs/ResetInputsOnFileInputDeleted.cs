using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Application.Commands.Inputs;
using MediatR;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class ResetInputsOnFileInputDeleted : INotificationHandler<FileInputDeletedEvent>
    {
        private readonly IMediator _mediator;

        public ResetInputsOnFileInputDeleted(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(FileInputDeletedEvent notification, CancellationToken cancellationToken)
        {
            switch (notification.InputType)
            {
                case FileInputType.DnaReport: break;
                case FileInputType.LabResults: break;
                case FileInputType.MicrobiomeData: await ClearMicrobiomeInputsAsync(notification.PatientId); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        #region private

        /// <summary>
        /// Sends command to clear microbiome inputs
        /// </summary>
        /// <param name="patientId"></param>
        private async Task ClearMicrobiomeInputsAsync(int patientId)
        {
            await _mediator.Send(new ResetMicrobiomeInputsCommand(patientId));
        }
        
        #endregion
    }
}