using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Commands.Agreements;
using WildHealth.Application.Services.Agreements;
using WildHealth.Domain.Entities.Agreements;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Services.Patients;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Agreements
{
    public class SignAgreementsCommandHandler : IRequestHandler<SignAgreementsCommand, AgreementConfirmation[]>
    {
        private readonly IPatientsService _patientsService;
        private readonly IAgreementsService _agreementsService;
        private readonly ITransactionManager _transactionManager;

        public SignAgreementsCommandHandler(
            IPatientsService patientsService,
            IAgreementsService agreementsService,
            ITransactionManager transactionManager)
        {
            _patientsService = patientsService;
            _agreementsService = agreementsService;
            _transactionManager = transactionManager;
        }

        public async Task<AgreementConfirmation[]> Handle(SignAgreementsCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.FetchPatientAsync(command.PatientId);

            var notSignedConfirmations = GetNotSignedConfirmations(patient);

            if (!notSignedConfirmations.Any())
            {
                throw new AppException(HttpStatusCode.BadRequest, "All agreements already signed.");
            }

            var signedConfirmations = new List<AgreementConfirmation>();

            await using (var transaction = _transactionManager.BeginTransaction())
            {
                try
                {
                    foreach (var confirmation in notSignedConfirmations)
                    {
                        var model = command.Confirmations.FirstOrDefault(x => x.AgreementId == confirmation.Agreement.Id);
                        if (model is null)
                        {
                            continue;
                        }

                        var signedConfirmation = await _agreementsService.SignAgreementAsync(
                            patient: patient,
                            confirmation: confirmation, 
                            model: model);
                
                        signedConfirmations.Add(signedConfirmation);
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }

            return signedConfirmations.ToArray();
        }
        
        #region private

        /// <summary>
        /// Returns not signed confirmations
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private AgreementConfirmation[] GetNotSignedConfirmations(Patient patient)
        {
            return patient.Agreements.Where(x => !x.IsSigned).ToArray();
        }
        
        #endregion
    }
}