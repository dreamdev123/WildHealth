using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildHealth.Application.Extensions;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Medications;
using WildHealth.Application.Services.Supplements;
using WildHealth.Application.Services.Timeline;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Common.Models.Attachments;
using WildHealth.Common.Models.Inputs;
using WildHealth.Common.Models.Medications;
using WildHealth.Common.Models.Notes;
using WildHealth.Common.Models.Patients;
using WildHealth.Common.Models.Supplements;
using WildHealth.Common.Models.VisitPrep;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using static WildHealth.Domain.Enums.Notes.NoteType;

namespace WildHealth.Application.Domain.VisitPrep;

public class VisitPrepAggregator : IVisitPrepAggregator
{
    private readonly INotesParser _notesParser;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public VisitPrepAggregator(
        INotesParser notesParser, 
        IServiceProvider serviceProvider, 
        IMapper mapper)
    {
        _notesParser = notesParser;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public async Task<VisitPrepModel> GetAsync(int patientId)
    {
        var providerPlanTask = GetProviderPlan(patientId);
        var hcPlanTask = GetHcPlan(patientId);
        var goalsTask = GetGoals(patientId);
        var medicationsTask = GetMedications(patientId);
        var supplementsTask = GetSupplements(patientId);
        var newLabsTask = GetNewLabs(patientId);
        var chronologicalSummaryEventsTask = GetChronologicalSummaryEvents(patientId);
        var recentDocumentsTask = GetRecentDocuments(patientId); 

        await Task.WhenAll(providerPlanTask, hcPlanTask, goalsTask, medicationsTask, supplementsTask, newLabsTask, chronologicalSummaryEventsTask, recentDocumentsTask);

        var (providerPlan, mdm) = providerPlanTask.Result;
        return new VisitPrepModel
        {
            ProviderPlan = providerPlan,
            HealthCoachPlan = hcPlanTask.Result,
            Mdm = mdm,
            Goals = goalsTask.Result,
            Medications = medicationsTask.Result,
            Supplements = supplementsTask.Result,
            NewLabs = newLabsTask.Result,
            ChronologicalSummaryEvents = chronologicalSummaryEventsTask.Result,
            RecentDocuments = recentDocumentsTask.Result
        };
    }

    private async Task<(VisitPrepNoteModel?, VisitPrepNoteModel?)> GetProviderPlan(int patientId)
    {
        var recentHPNote = await GetRecentHpNote(patientId);

        if (recentHPNote == null)
            return (null, null);

        var provider = new VisitPrepNoteModel
        {
            NoteId = recentHPNote.GetId(),
            Text = _notesParser.ParsePlanText(recentHPNote.Content)
        };
        
        var mdm = new VisitPrepNoteModel
        {
            NoteId = recentHPNote.GetId(),
            Text = _notesParser.ParseMdm(recentHPNote.Content)
        };
        
        return (provider, mdm);
    }
    
    private async Task<VisitPrepNoteModel?> GetHcPlan(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var notesRepository = scope.ServiceProvider.GetRequiredService<IGeneralRepository<Note>>();
        
        var recentHCNote = await notesRepository
            .All()
            .IncludeContent()
            .RelatedToPatient(patientId)
            .OnlyCompleted()
            .ByTypes(new[] { Initial, FollowUp })
            .OrderByDescending(x => x.VisitDate)
            .FirstOrDefaultAsync();
        
        if (recentHCNote == null)
            return null;

        return new VisitPrepNoteModel()
        {
            NoteId = recentHCNote.GetId(),
            Text = _notesParser.ParseHcPlanText(recentHCNote.Content, recentHCNote.Type)
        };
    }

    private async Task<NoteGoalModel[]> GetGoals(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var notesRepository = scope.ServiceProvider.GetRequiredService<IGeneralRepository<Note>>();
        var lastNote = await notesRepository
            .All()
            .IncludeContent()
            .RelatedToPatient(patientId)
            .OnlyCompleted()
            .ByTypes(new[] { FollowUp, HistoryAndPhysicalInitial, HistoryAndPhysicalFollowUp, HistoryAndPhysicalGroupVisit })
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        if (lastNote == null)
            return Array.Empty<NoteGoalModel>();

        var goals = _notesParser.ParseGoals(lastNote);

        return goals;
    }

    private async Task<PatientMedicationModel[]> GetMedications(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var patientsMedicationsService = scope.ServiceProvider.GetRequiredService<IPatientMedicationsService>();
        var result = await patientsMedicationsService.GetAsync(patientId);
        return _mapper.Map<List<PatientMedicationModel>>(result).ToArray();
    }
    
    private async Task<PatientSupplementModel[]> GetSupplements(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var patientsMedicationsService = scope.ServiceProvider.GetRequiredService<IPatientsSupplementsService>();
        var result = await patientsMedicationsService.GetAsync(patientId);
        return _mapper.Map<List<PatientSupplementModel>>(result).ToArray();
    }

    private async Task<LabInputModel[]> GetNewLabs(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var inputsService = scope.ServiceProvider.GetRequiredService<IInputsService>();
        var appointmentsRepository = scope.ServiceProvider.GetRequiredService<IGeneralRepository<Appointment>>();
        
        var allInputs = await inputsService.GetLabInputsAsync(patientId);
        var lastMedicalVisitDate = await appointmentsRepository.All()
            .RelatedToPatient(patientId)
            .Where(x => x.WithType == AppointmentWithType.Provider || x.WithType == AppointmentWithType.HealthCoachAndProvider)
            .Where(x => x.Status == AppointmentStatus.Submitted)
            .Where(x => x.StartDate < DateTime.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .Select(x => x.StartDate)
            .FirstOrDefaultAsync();
        
        var recentInputs = lastMedicalVisitDate == default ? 
            allInputs : 
            allInputs.Where(x => (x.ModifiedAt ?? x.CreatedAt) > lastMedicalVisitDate);

        return _mapper.Map<List<LabInputModel>>(recentInputs).Where(x => x.IsHighlighted).ToArray();
    }
    
    private async Task<PatientTimelineEventModel[]> GetChronologicalSummaryEvents(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IPatientTimelineQueryService>();

        return (await queryService.GetTimelineEvents(patientId))
            .Select(e => e.GeneralPresentation())
            .ToArray();
    }
    
    private async Task<AttachmentModel[]> GetRecentDocuments(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IAttachmentsService>();
        var allDocs = await queryService.GetByPatientIdAsync(patientId);
        var usersRepo = scope.ServiceProvider.GetRequiredService<IGeneralRepository<User>>();
      
        var recentHPNote = await GetRecentHpNote(patientId);
        var recentDocs = recentHPNote == null ? 
            allDocs : 
            allDocs.Where(d => d.CreatedAt > recentHPNote.VisitDate);

        var createdByIds = recentDocs.Select(x => x.CreatedBy).ToArray();
        var users = await usersRepo.All().ByIds(createdByIds).Select(u => new { Id = u.Id!.Value, FullName = $"{u.FirstName} {u.LastName}" }).ToArrayAsync();
        var createdByLookup = users.ToDictionary(x => x.Id, x => x.FullName);

        var result = _mapper.Map<List<AttachmentModel>>(recentDocs).ToArray();
        result.ForEach(x => x.UploadedBy = createdByLookup[x.UploadedById]);
        
        return result;
    }
    
    private async Task<Note?> GetRecentHpNote(int patientId)
    {
        using var scope = _serviceProvider.CreateScope();
        var notesRepository = scope.ServiceProvider.GetRequiredService<IGeneralRepository<Note>>();
        var recentHPNote = await notesRepository
            .All()
            .IncludeContent()
            .RelatedToPatient(patientId)
            .OnlyCompleted()
            .ByTypes(new[] { Soap, HistoryAndPhysicalInitial, HistoryAndPhysicalFollowUp, HistoryAndPhysicalGroupVisit })
            .Select(x => new { note = x, startDate = x.AppointmentId.HasValue ? x.Appointment.StartDate : x.VisitDate })
            .OrderByDescending(x => x.startDate)
            .Select(x => x.note)
            .FirstOrDefaultAsync();
        
        return recentHPNote;
    }
}