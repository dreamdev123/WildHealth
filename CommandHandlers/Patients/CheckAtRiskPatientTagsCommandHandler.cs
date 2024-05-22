using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Constants;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class CheckAtRiskPatientTagsCommandHandler : IRequestHandler<CheckAtRiskPatientTagsCommand>
    {
        private const string AtRiskIccTagName = TagsConstants.AtICCRisk;
        private const string AtRiskImcTagName = TagsConstants.AtIMCRisk;
        private readonly IPatientsService _patientsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CheckAtRiskPatientTagsCommandHandler(
            IPatientsService patientsService,
            IMediator mediator,
            ILogger<CheckAtRiskPatientTagsCommandHandler> logger)
        {
            _patientsService = patientsService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(CheckAtRiskPatientTagsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting untagging of at risk for patient [Id]: {command.Patient.Id}");

            try
            {
                var patient = command.Patient;

                var iccRiskPatients = await _patientsService.AtRiskIccDue();

                if (iccRiskPatients.All(rp => rp.PatientId != patient.Id))
                {
                    var deleteIccCommand = new RemoveTagCommand(patient, AtRiskIccTagName);
                    
                    await _mediator.Send(deleteIccCommand, cancellationToken);
                }
                
                var imcRiskPatients = await _patientsService.AtRiskImcDue();

                if (imcRiskPatients.All(rp => rp.PatientId != patient.Id))
                {
                    var deleteIccCommand = new RemoveTagCommand(patient, AtRiskImcTagName);
                    
                    await _mediator.Send(deleteIccCommand, cancellationToken);
                }

                _logger.LogInformation($"Finished untagging of at risk for patient [id]: {command.Patient.Id} ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to run the at risk untagging command - {ex}");
            }
        }
    }
}