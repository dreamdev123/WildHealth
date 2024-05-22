using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.AdminTools;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.AdminTools;

namespace WildHealth.Application.CommandHandlers.AdminTools;

public class GetPaymentPlansNamesAndPatientTagsCommandHandler:IRequestHandler<GetPaymentPlansNamesAndPatientTagsCommand,PlansNamesAndTagsModel>
{
    private readonly IPatientsService _patientsService;

    public GetPaymentPlansNamesAndPatientTagsCommandHandler(
        IPatientsService patientsService)
    {
        _patientsService = patientsService;
    }

    public async Task<PlansNamesAndTagsModel> Handle(GetPaymentPlansNamesAndPatientTagsCommand request, CancellationToken cancellationToken)
    {
        var allPatients = await _patientsService.GetAllMyPatientsNoFilter();

        var result = new PlansNamesAndTagsModel
        {
            PaymentPlans = allPatients.Select(x => x.ActivePlan).Select(x => x.Name).Distinct().ToArray(),
            Tags = allPatients.SelectMany(x => x.Tags).Select(x => x.Name).Distinct().ToArray()
        };

        return result;
    }
}