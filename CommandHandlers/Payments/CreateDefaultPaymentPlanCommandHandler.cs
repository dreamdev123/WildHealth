using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Data.Context;
using WildHealth.Shared.Data.Repository;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Payments;

public class CreateDefaultPaymentPlanCommandHandler : IRequestHandler<CreateDefaultPaymentPlanCommand, PaymentPlan>
{
    private readonly IDictionary<int, bool> _defaultAddOnIds = new Dictionary<int, bool>
    {
        { 1, false }, 
        { 2, false },
        { 3, true },
        { 8000, false }, 
        { 8001, false }
    };
    
    // Prevision care package should have the DNA and advanced blood analysis both required by default
    // It's assumed that an employer flow will take care of paying for the blood analysis
    private readonly IDictionary<int, bool> _defaultPrecisionCarePackageAddOnIds = new Dictionary<int, bool>
    {
        { 2, false },
        { 3, true },
        { 8000, true }, 
        { 8001, true }
    };
    
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IGeneralRepository<Inclusion> _inclusionRepository;
    private readonly IGeneralRepository<AddOn> _addOnRepository;
    private readonly IGeneralRepository<AppointmentTypeConfigurationPaymentPlan> _appointmentTypeConfigurationsRepository;
    private readonly DbContext _dbContext;
    
    private readonly ILogger<CreateDefaultPaymentPlanCommandHandler> _logger;

    public CreateDefaultPaymentPlanCommandHandler(
        IPaymentPlansService paymentPlansService, 
        IGeneralRepository<Inclusion> inclusionRepository,
        IGeneralRepository<AddOn> addOnRepository,
        IGeneralRepository<AppointmentTypeConfigurationPaymentPlan> appointmentTypeConfigurationsRepository,
        ILogger<CreateDefaultPaymentPlanCommandHandler> logger,
        IApplicationDbContext applicationDbContext
        )
    {
        _paymentPlansService = paymentPlansService;
        _inclusionRepository = inclusionRepository;
        _addOnRepository = addOnRepository;
        _appointmentTypeConfigurationsRepository = appointmentTypeConfigurationsRepository;
        _logger = logger;
        _dbContext = applicationDbContext.Instance;
    }

    public async Task<PaymentPlan> Handle(CreateDefaultPaymentPlanCommand command, CancellationToken cancellationToken)
    {
        // Assert proper configuration for a precision care package flow
        if (command.IsPrecisionCarePackageFlow && !command.CanBeActivated)
        {
            throw new Exception(
                $"It's expected that if it's a precision care package flow that the subscription can be activated, detected conflict");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await TryUpdateIdentityInsert("dbo.PaymentPlans", "ON");

            var addOnIds = _defaultAddOnIds.Select(o => o.Key);
            var precisionCarePackageAddOnIds = _defaultPrecisionCarePackageAddOnIds.Select(o => o.Key);
            var appointmentConfigurations = await _appointmentTypeConfigurationsRepository
                .All()
                .Where(x => x.PaymentPlanId == command.PaymentPlanTemplateId)
                .ToArrayAsync(cancellationToken);
            
            var addOns = await _addOnRepository
                .All()
                .Where(o => 
                    command.IsPrecisionCarePackageFlow ? 
                        precisionCarePackageAddOnIds.Contains(o.Id!.Value) : 
                        addOnIds.Contains(o.Id!.Value))
                .ToArrayAsync(cancellationToken: cancellationToken);

            // var paymentPlanAddOnIndex = command.DesiredId;
            var paymentPlanAddOns = command.IncludeDefaultAddOns
                ? addOns.Select(o => new PaymentPlanAddOn
                {
                    // Id = paymentPlanAddOnIndex++,
                    AddOnId = o.GetId(),
                    Required = _defaultAddOnIds[o.GetId()]
                })
                : Enumerable.Empty<PaymentPlanAddOn>();

            var inclusions = command.Inclusions.Select(o =>
            {
                var matchingInclusion = _inclusionRepository
                    .All()
                    .FirstOrDefaultAsync(a =>
                        a.ProductType == o.ProductType &&
                        a.Count == o.Count &&
                        a.Option == o.Option).Result;

                // If we don't have it then we need to create the inclusion
                if (matchingInclusion is null)
                {
                    matchingInclusion = new Inclusion
                    {
                        ProductType = o.ProductType,
                        Count = o.Count,
                        Option = o.Option
                    };

                    _inclusionRepository.AddAsync(matchingInclusion);
                }

                return matchingInclusion;
            }).ToArray();

            var paymentPeriodInclusions = inclusions.Select(o =>
            {
                return new PaymentPeriodInclusion
                {
                    Inclusion = o
                };
            });

            // var paymentPriceIndex = command.DesiredId;
            var paymentPrices = command.Prices.Select(o =>
            {
                return new PaymentPrice
                {
                    // Id = paymentPriceIndex++,
                    Strategy = o.Strategy,
                    StartupFee = o.StartupFee,
                    OriginalPrice = o.OriginalPrice,
                    Discount = o.Discount,
                    Price = o.Price,
                    IsActive = true,
                    DiscountInsurance = o.DiscountInsurance,
                    Type = o.Type,
                    IsAutoRenewal = o.IsAutoRenewal,
                    Integrations = new List<PaymentPriceIntegration>
                    {
                        new PaymentPriceIntegration(
                            vendor: IntegrationVendor.Stripe,
                            purpose: IntegrationPurposes.Payment.ProductId,
                            value: command.StripeProductId)
                    }
                };
            });

            var paymentPlan = new PaymentPlan
            {
                Id = command.DesiredId,
                Name = command.Name,
                DisplayName = command.DisplayName,
                Title = command.Title,
                IsActive = command.IsActive,
                PracticeId = command.PracticeId,
                IsSingle = true,
                PaymentPlanAddOns = new List<PaymentPlanAddOn>(paymentPlanAddOns),
                PaymentPeriods = new List<PaymentPeriod>(),
                CanBeActivated = command.CanBeActivated,
                AppointmentTypeConfigurations = appointmentConfigurations.Select(x => new AppointmentTypeConfigurationPaymentPlan
                {
                    AppointmentTypeConfigurationId = x.AppointmentTypeConfigurationId
                }).ToArray()
            };

            // var paymentPeriodIndex = command.DesiredId;
            paymentPlan.PaymentPeriods.Add(new PaymentPeriod
            {
                // Id = paymentPeriodIndex++,
                PeriodInMonths = command.PeriodInMonths,
                IsActive = true,
                Order = 1,
                PaymentPeriodInclusions = new List<PaymentPeriodInclusion>(paymentPeriodInclusions),
                Prices = new List<PaymentPrice>(paymentPrices),
            });

            await _paymentPlansService.CreatePaymentPlanAsync(paymentPlan);
                
            await TryUpdateIdentityInsert("dbo.PaymentPlans", "OFF");
                
            await transaction.CommitAsync(cancellationToken);

            return paymentPlan;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError($"Problem creating [PaymentPlanName] = {command.Name}, {ex}");

            throw;
        }
    }

    private async Task TryUpdateIdentityInsert(string table, string value)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {table} {value}");
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Problem updating IDENTITY_INSERT for [Table] = {table} to [Value] = {value}", ex);
        }
    }
}