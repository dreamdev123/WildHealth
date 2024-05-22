using System.Collections.Generic;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.HealthLogs;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthLogs
{
    public class GetAllHealthLogsCommand : IRequest<IEnumerable<AllQuestionnairesModel>>, IValidatabe
    {
        public int PatientId { get; }

        public int PracticeId { get; }

        public GetAllHealthLogsCommand(int patientId, int practiceId)
        {
            PatientId = patientId;
            PracticeId = practiceId;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<GetAllHealthLogsCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PatientId).GreaterThan(0);

                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }
    }
}
