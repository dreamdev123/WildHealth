using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.Tags;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class CreateInsuranceVerificationCommandHandler: IRequestHandler<CreateInsuranceVerificationCommand, InsuranceVerification>
{
    private readonly IInsuranceVerificationService _insuranceVerificationService;
    private readonly ITransactionManager _transactionManager;
    private readonly ICoveragesService _coveragesService;
    private readonly IPatientsService _patientsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediator _mediator;
    
    public CreateInsuranceVerificationCommandHandler(
        IInsuranceVerificationService insuranceVerificationService,
        ITransactionManager transactionManager,
        ICoveragesService coveragesService,
        IDateTimeProvider dateTimeProvider,
        IPatientsService patientsService,
        IMediator mediator)
    {
        _insuranceVerificationService = insuranceVerificationService;
        _transactionManager = transactionManager;
        _coveragesService = coveragesService;
        _dateTimeProvider = dateTimeProvider;
        _patientsService = patientsService;
        _mediator = mediator;
    }

    public async Task<InsuranceVerification> Handle(CreateInsuranceVerificationCommand command, CancellationToken cancellationToken)
    {
        var coverage = await _coveragesService.GetAsync(command.CoverageId);

        var patient = await _patientsService.GetByIdAsync(command.PatientId);
        
        var insuranceVerification = new InsuranceVerification(
            patient: patient,
            runAt: _dateTimeProvider.UtcNow(),
            isVerified: command.IsVerified,
            copay: command.Copay,
            coverage: coverage,
            errorCode: command.ErrorCode,
            raw271: command.Raw271
        );

        await using var transaction = _transactionManager.BeginTransaction();
        
        try
        {
            var result = await _insuranceVerificationService.CreateAsync(insuranceVerification);
        
            var tag = command.IsVerified
                ? Common.Constants.Tags.InsuranceVerified
                : Common.Constants.Tags.InsuranceNotVerified;
        
            await _mediator.Send(new CreateTagCommand(patient, tag), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
                
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
                
            throw;
        }
    }
}