using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.Spreadsheets;
using WildHealth.Shared.Data.Managers.TransactionManager;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UploadFileChangeHealthCoachCommandHandler : IRequestHandler<UploadFileChangeStaffCommand>
    {
        private const string PatientEmail = "Patient Email";
        private const string PriorHealthCoachEmail = "Prior HC Email";
        private const string NewHealthCoachEmail = "New HC Email";
        private const string PriorProviderEmail = "Prior Provider Email";
        private const string NewProviderEmail = "New Provider Email";
        private const string AttemptToRescheduleIndividualAppointments = "Should Reschedule";
        private const string AttemptToRescheduleInDifferentTimeSlot = "Should Reschedule If Conflict";
        private const string ShouldSendChangeMessageToPatient = "Should Message";

        private readonly IUsersService _usersService;
        private readonly IMediator _mediator;
        private readonly ILogger<UploadFileChangeHealthCoachCommandHandler> _logger;
        private readonly ITransactionManager _transactionManager;
        private readonly IMapper _mapper;

        public UploadFileChangeHealthCoachCommandHandler(
            IUsersService usersService,
            IMediator mediator,
            ILogger<UploadFileChangeHealthCoachCommandHandler> logger,
            ITransactionManager transactionManager,
            IMapper mapper
            )
        {
            _usersService = usersService;
            _mediator = mediator;
            _logger = logger;
            _transactionManager = transactionManager;
            _mapper = mapper;
        }

        public async Task Handle(UploadFileChangeStaffCommand command, CancellationToken cancellationToken)
        {
            var spreadsheetIterator = new SpreadsheetIterator(command.File);
            
            var importantTitles = new Dictionary<string, string>
            {
                { PatientEmail, string.Empty },
                { PriorHealthCoachEmail, string.Empty },
                { NewHealthCoachEmail, string.Empty },
                { PriorProviderEmail, string.Empty },
                { NewProviderEmail, string.Empty },
                { AttemptToRescheduleIndividualAppointments, string.Empty },
                { AttemptToRescheduleInDifferentTimeSlot, string.Empty },
                { ShouldSendChangeMessageToPatient, string.Empty },
            };

            try
            {
                await spreadsheetIterator.Iterate(importantTitles, async (rowResults) =>
                {
                    var patientEmail = rowResults[PatientEmail];
                    var priorHealthCoachEmail = rowResults[PriorHealthCoachEmail];
                    var newHealthCoachEmail = rowResults[NewHealthCoachEmail];
                    var priorProviderEmail = rowResults[PriorProviderEmail];
                    var newProviderEmail = rowResults[NewProviderEmail];
                    var attemptToRescheduleIndividualAppointments = Convert.ToBoolean(rowResults[AttemptToRescheduleIndividualAppointments]);
                    var attemptToRescheduleInDifferentTimeSlot = Convert.ToBoolean(rowResults[AttemptToRescheduleInDifferentTimeSlot]);
                    var shouldSendChangeMessageToPatient = Convert.ToBoolean(rowResults[ShouldSendChangeMessageToPatient]);

                    await _mediator.Send(new ChangeStaffCommand(
                        PatientEmail: patientEmail,
                        FromHealthCoachEmail: priorHealthCoachEmail,
                        ToHealthCoachEmail: newHealthCoachEmail,
                        FromProviderEmail: priorProviderEmail,
                        ToProviderEmail: newProviderEmail,
                        AttemptToRescheduleIndividualAppointments: attemptToRescheduleIndividualAppointments,
                        ShouldSendChangeMessageToPatient: shouldSendChangeMessageToPatient,
                        AttemptToRescheduleInDifferentTimeSlot: attemptToRescheduleInDifferentTimeSlot));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Failed to change health coaches supplied patients, {ex}");

                throw;
            }
        }
    }
}