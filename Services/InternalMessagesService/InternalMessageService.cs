using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Messages;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.InternalMessagesService;

public class InternalMessageService : IInternalMessagesService
{
    private readonly IGeneralRepository<Message> _messageRepository;
    private readonly IPatientsService _patientsService;

    public InternalMessageService(
        IGeneralRepository<Message> messageRepository, 
        IPatientsService patientsService)
    {
        _messageRepository = messageRepository;
        _patientsService = patientsService;
    }
    
    public async Task<(ICollection<Message>, int)> GetPageAsync(int page, int pageSize, string? searchQuery)
    {
        var queryData = _messageRepository
            .All()
            .Include(x => x.Employee)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Where(x => string.IsNullOrEmpty(searchQuery)
                        || (x.Employee.User.FirstName + " " + x.Employee.User.LastName).Contains(searchQuery)
                        || searchQuery.Contains(x.Employee.User.FirstName + " " + x.Employee.User.LastName)
                        || x.Employee.User.Email.Contains(searchQuery)
                        || searchQuery.Contains(x.Employee.User.Email)
                        || x.Id.ToString() == searchQuery);
        
        
        var totalCount = await queryData.CountAsync();
        var messages = await queryData.Pagination(page * pageSize, pageSize).ToArrayAsync();
        
        return (messages, totalCount);
    }

    public async Task<Message> GetByIdAsync(int id)
    {
       return await _messageRepository
           .All()
           .ById(id)
           .FirstAsync();
    }

    public async Task<Message> CreateMessageAsync(Message message)
    {
        await _messageRepository.AddAsync(message);

        await _messageRepository.SaveAsync();

        return message;
    }

    public async Task<ICollection<int>> GetPatientIdsByFilterAsync(MyPatientsFilterModel myPatientsFilter)
    {
        var patients = await _patientsService.GetMyPatientsWithFiltersWithoutAssigment(myPatientsFilter);
        
        var patientIdsArray = patients.Select(x => x.PatientId).ToArray();

        return patientIdsArray;
    }

    public async Task<Message> UpdateMessageAsync(Message message)
    {
        _messageRepository.Edit(message);

        await _messageRepository.SaveAsync();

        return message;
    }
}