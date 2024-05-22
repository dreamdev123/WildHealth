using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Domain.Entities.EmployerProducts;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.EmployerProducts;
using WildHealth.Domain.Entities.Products;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Payments;

public class CreateEmployerProductCommandHandler : IRequestHandler<CreateEmployerProductCommand, EmployerProduct>
{
    private readonly IEmployerProductService _employerProductService;

    public CreateEmployerProductCommandHandler(IEmployerProductService employerProductService)
    {
        _employerProductService = employerProductService;
    }

    public async Task<EmployerProduct> Handle(CreateEmployerProductCommand command, CancellationToken cancellationToken)
    {
        await AssertEmployerProductNotFound(command);
        
        var employerProduct = new EmployerProduct
        {
            Key = command.Key,
            Name = command.Name,
            IsLimited = command.IsLimited,
            Description = command.Description,
            BannerCdnUrl = command.BannerCdnUrl,
            SupportedPaymentPriceIds = command.SupportedPaymentPriceIds,
            UniversalId = Guid.NewGuid(),
            Settings = CreateSettings(command.Settings),
            Discounts = command.Discounts.Select(x => new EmployerChargeItem
                {
                    ProductType = x.ProductType,
                    ProductTypeId = x.ProductTypeId,
                    RequiresProductTypeId = x.RequiresProductTypeId,
                    DiscountType = x.DiscountType,
                    EmployerDiscountType = x.EmployerDiscountType,
                    PatientDiscount = x.PatientDiscount,
                    EmployerDiscount = x.EmployerDiscount,
                }).ToArray(),
            Inclusions = command.Inclusions.Select(x => new EmployerInclusion
            {
                Inclusion = new Inclusion()
                {
                    ProductType = x.ProductType,
                    Option = x.Option,
                    Price = x.Price,
                    Count = x.Count
                }
            }).ToArray()
        };

        await _employerProductService.CreateAsync(employerProduct);

        return employerProduct;
    }
    
    #region private

    private async Task AssertEmployerProductNotFound(CreateEmployerProductCommand command)
    {
        try
        {
            await _employerProductService.GetByKeyAsync(command.Key);
            
            throw new AppException(HttpStatusCode.BadRequest, "Employer product with this Key already exists.");
        }
        catch (AppException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            // Continue, we expect to not find the employer product so we can create it
        }
    }
    
    /// <summary>
    /// Creates employer settings
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private EmployerSettings[] CreateSettings(EmployerProductSettingsModel model)
    {
        var data = new List<EmployerSettings>();

        var type = model.GetType();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(model, null)?.ToString();
            
            data.Add(new EmployerSettings
            {
                Key = property.Name,
                Value = value
            });
        }

        return data.ToArray();
    }

    #endregion
}