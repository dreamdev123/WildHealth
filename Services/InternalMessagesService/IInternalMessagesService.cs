using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Messages;

namespace WildHealth.Application.Services.InternalMessagesService;

public interface IInternalMessagesService
{
    /// <summary>
    /// Return page with messages
    /// </summary>
    /// <returns></returns>
    public Task<(ICollection<Message>, int)> GetPageAsync(int page, int pageSize, string? searchQuery);

    /// <summary>
    /// return message by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<Message> GetByIdAsync(int id);
    
    /// <summary>
    /// Creates message
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task<Message> CreateMessageAsync(Message message);

    /// <summary>
    /// Returns all patient ids by filter
    /// </summary>
    /// <param name="myPatientsFilter"></param>
    /// <returns></returns>
    public Task<ICollection<int>> GetPatientIdsByFilterAsync(MyPatientsFilterModel myPatientsFilter);
    
    /// <summary>
    /// Updates message (status)
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public Task<Message> UpdateMessageAsync(Message message);
}