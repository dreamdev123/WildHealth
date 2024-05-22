using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.EmployerProducts;
using WildHealth.Domain.Entities.EmployerProducts;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments;

public class CreateEmployerProductCommand : IRequest<EmployerProduct>, IValidatabe
{
    public string Name { get; }
    
    public string Key { get; }
    
    public bool IsLimited { get; }

    public string Description { get; }
    
    public string BannerCdnUrl { get; }
    
    public int[] SupportedPaymentPriceIds { get; }

    public EmployerChargeItemModel[] Discounts { get; }
    
    public EmployerInclusionModel[] Inclusions { get; }
    
    public EmployerProductSettingsModel Settings { get; }
    
    public CreateEmployerProductCommand(
        string name, 
        string key, 
        bool isLimited,
        string description,
        string bannerCdnUrl,
        int[] supportedPaymentPriceIds,
        EmployerChargeItemModel[] discounts, 
        EmployerInclusionModel[] inclusions, 
        EmployerProductSettingsModel settings)
    {
        Name = name;
        Key = key;
        IsLimited = isLimited;
        Description = description;
        BannerCdnUrl = bannerCdnUrl;
        SupportedPaymentPriceIds = supportedPaymentPriceIds;
        Discounts = discounts;
        Inclusions = inclusions;
        Settings = settings;
    }

    #region private

    private class Validator : AbstractValidator<CreateEmployerProductCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty();
            RuleFor(x => x.Key).NotNull().NotEmpty();
            RuleFor(x => x.Discounts).NotNull().NotEmpty();
            // RuleFor(x => x.Inclusions).NotNull().NotEmpty();
        }
    }

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);
    
    #endregion
}