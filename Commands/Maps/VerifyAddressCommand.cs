using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Lob.Clients.Models;

namespace WildHealth.Application.Commands.Maps;

public class VerifyAddressCommand : IRequest<LobVerifyAddressResponseModel>
{
    public string? FullAddress { get; }
    public string? StreetAddress1 { get; }
    public string? StreetAddress2 { get; }
    public string? City { get; }
    public string? StateAbbreviation { get; }
    public string? ZipCode { get; }

    public VerifyAddressCommand(string streetAddress1, string streetAddress2, string city, string stateAbbreviation, string zipCode)
    {
        StreetAddress1 = streetAddress1;
        StreetAddress2 = streetAddress2;
        City = city;
        StateAbbreviation = stateAbbreviation;
        ZipCode = zipCode;
    }

    public VerifyAddressCommand(string fullAddress)
    {
        FullAddress = fullAddress;
    }
}