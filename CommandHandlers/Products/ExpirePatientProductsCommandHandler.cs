using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Utils.DateTimes;
using MediatR;
using WildHealth.Application.CommandHandlers.Products.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;

namespace WildHealth.Application.CommandHandlers.Products;

public class ExpirePatientProductsCommandHandler : IRequestHandler<ExpirePatientProductsCommand>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;

    public ExpirePatientProductsCommandHandler(
        IPatientProductsService patientProductsService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializeFlow)
    {
        _patientProductsService = patientProductsService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }
    
    public async Task Handle(ExpirePatientProductsCommand request, CancellationToken cancellationToken)
    {
        var productsToExpire = await _patientProductsService.GetBuiltInByPatientAsync(request.PatientId);
        var date = _dateTimeProvider.UtcNow();
        
        var flow = new ExpirePatientProductsFlow(productsToExpire, request.Reason, date);
        await flow.Materialize(_materializeFlow);
    }
}