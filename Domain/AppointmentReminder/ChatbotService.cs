using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Domain.AppointmentReminder.Bot;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Chatbots;
using WildHealth.Shared.Data.Repository;
using static WildHealth.Application.Domain.AppointmentReminder.Bot.AppointmentReminderData;

namespace WildHealth.Application.Domain.AppointmentReminder;

public interface IAppointmentReminderService
{
    Task<List<AppointmentReminderData>> GetAllAsync(int patientId, ChatbotType chatbotType);
}

public class AppointmentReminderService : IAppointmentReminderService
{
    private readonly IGeneralRepository<Chatbot> _chatbotRepository;
    private readonly IGeneralRepository<Appointment> _appointmentRepository;

    public AppointmentReminderService(
        IGeneralRepository<Chatbot> chatbotRepository, 
        IGeneralRepository<Appointment> appointmentRepository)
    {
        _chatbotRepository = chatbotRepository;
        _appointmentRepository = appointmentRepository;
    }

    public async Task<List<AppointmentReminderData>> GetAllAsync(int patientId, ChatbotType chatbotType)
    {
        var bots = await _chatbotRepository.All()
            .Where(x => x.PatientId == patientId && x.Type == chatbotType)
            .ToListAsync();

        var appointmentIds = bots.Select(GetAppointmentId).ToArray();

        var appointments = await _appointmentRepository.All().Where(x => appointmentIds.Contains(x.Id!.Value)).ToListAsync();
        
        return bots.Select(b => new AppointmentReminderData(b, LookupAppointment(b, appointments))).ToList();
    }
}
