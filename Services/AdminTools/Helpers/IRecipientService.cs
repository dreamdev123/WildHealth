using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.AdminTools.Helpers;

public interface IRecipientService
{
    Task<ICollection<User>> GetAllRecipientsByFilter(MyPatientsFilterModel patientsFilterModel);
}