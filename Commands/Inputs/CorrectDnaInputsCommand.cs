using MediatR;

namespace WildHealth.Application.Commands.Inputs;

public class CorrectDnaInputsCommand : IRequest
{
    public string Barcode { get; }
    
    public CorrectDnaInputsCommand(string barcode)
    {
        Barcode = barcode;
    }
}