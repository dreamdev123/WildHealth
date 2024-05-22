using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Messages;
using WildHealth.IntegrationEvents.Messages.Payloads;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class ParseSpecialTestsFlow: IMaterialisableFlow
{
    private readonly Patient _patient;
    private readonly Employee _employee;
    private readonly NoteSpecialTestModel[] _specialTests;
    private readonly DateTime _now;

    public ParseSpecialTestsFlow(
        Patient patient, 
        Employee employee,
        NoteSpecialTestModel[] specialTests,
        DateTime now)
    {
        _patient = patient;
        _employee = employee;
        _specialTests = specialTests;
        _now = now;
    }

    public MaterialisableFlowResult Execute()
    {
        if (!_specialTests.Any())
        {
            return MaterialisableFlowResult.Empty;
        }

        var events = new List<BaseIntegrationEvent>();

        foreach (var test in _specialTests)
        {
            var messageEvent = new MessageIntegrationEvent(new SlackMessagePayload(
                message: BuildOrderMessage(test),
                messageType: "SpecialTestsOrdered"
            ), _now);
            
            events.Add(messageEvent);
        }
        
        return new MaterialisableFlowResult(Array.Empty<EntityAction>(), events);
    }
    
    #region private

    private string BuildOrderMessage(NoteSpecialTestModel test)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append($"Patient Id: {_patient.GetId()}\n");
        stringBuilder.Append($"Test Ordered: {test.Name} - {test.Company}");

        if (!string.IsNullOrEmpty(test.Cost))
        {
            stringBuilder.Append($" - {test.Cost}");
        }

        stringBuilder.Append("\n");
        stringBuilder.Append($"Ordering Physician: {_employee.User.GetFullname()}");

        return stringBuilder.ToString();

    }
    
    #endregion
}