using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Application.Services.InternalMessagesService;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.Services.AdminTools.Helpers;

public class RecipientService : IRecipientService
{
    private readonly IInternalMessagesService _messagesService;
    private readonly IPatientsService _patientsService;

    public RecipientService(IInternalMessagesService messagesService, IPatientsService patientsService)
    {
        _messagesService = messagesService;
        _patientsService = patientsService;
    }
    
    public async Task<ICollection<User>> GetAllRecipientsByFilter(MyPatientsFilterModel filterModel)
    {
        var patientIds = await _messagesService.GetPatientIdsByFilterAsync(filterModel);

        var allUsers = new List<User>();
        
        foreach (var patientId in patientIds)
        {
            var patient = await _patientsService.GetByIdAsync(patientId, PatientSpecifications.PatientUserDevicesSpecification);
            
            allUsers.Add(patient.User);
        }

        return allUsers;
    }
}