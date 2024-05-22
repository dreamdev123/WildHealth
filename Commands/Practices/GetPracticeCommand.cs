using MediatR;
using WildHealth.Common.Models.Practices;
using System.Collections.Generic;

namespace WildHealth.Application.Commands.Practices
{
    public class GetPracticeCommand : IRequest<List<PracticeModel>>
    {
    
        public GetPracticeCommand()
        {
           
        }

    }
}
