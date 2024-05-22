using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Vitals;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Utils.NotesParser;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Vitals;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class ParseVitalsFromNoteCommandHandler : IRequestHandler<ParseVitalsFromNoteCommand>
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IVitalService _vitalService;
    private readonly INotesParser _notesParser;

    public ParseVitalsFromNoteCommandHandler(IDateTimeProvider dateTimeProvider, IVitalService vitalService, INotesParser notesParser)
    {
        _dateTimeProvider = dateTimeProvider;
        _vitalService = vitalService;
        _notesParser = notesParser;
    }

    public async Task Handle(ParseVitalsFromNoteCommand command, CancellationToken cancellationToken)
    {
        var date = _dateTimeProvider.UtcNow();
        var note = command.Note;
        
        try
        {
            await _vitalService.AssertDateAsync(note.PatientId, date);
        }
        catch (AppException e) when(e.StatusCode == HttpStatusCode.NotAcceptable)
        {
            return;
        }

        var physicalData = _notesParser.ParsePhysicalData(note);

        if (physicalData is null || physicalData.UnableToObtain)
        {
            return;
        }

        var values = new Dictionary<string, decimal?>
        {
            { VitalNames.Height.Name, physicalData.Height },
            { VitalNames.Weight.Name, physicalData.Weight },
            { VitalNames.SystolicBloodPressure.Name, physicalData.SystolicBP },
            { VitalNames.DiastolicBloodPressure.Name, physicalData.DiastolicBP },
            { VitalNames.Temperature.Name, physicalData.Temperature },
            { VitalNames.HeartRate.Name, physicalData.HeartRate },
        };

        var vitals = new List<CreateVitalModel>();

        foreach (var (name, value) in values)
        {
            if (value is > 0)
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = name,
                    Value = value.Value,
                    DateTime = date,
                    SourceType = VitalValueSourceType.HistoryAndPhysicalNote
                });
            }
        }

        if (vitals.Any())
        {
            await _vitalService.ParseAsync(note.PatientId, vitals);
        }
    }
}