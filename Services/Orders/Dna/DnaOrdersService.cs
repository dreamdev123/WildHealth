using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.Patients;
using WildHealth.Common.Models.Integration.ImageMark;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Orders.Dna
{
    /// <summary>
    /// <see cref="IDnaOrdersService"/>
    /// </summary>
    public class DnaOrdersService : IDnaOrdersService
    {
        private readonly IGeneralRepository<DnaOrder> _dnaOrdersRepository;
        private readonly IMapper _mapper;
        private readonly IPatientCohortHelper _patientCohortHelper;

        public DnaOrdersService(IGeneralRepository<DnaOrder> dnaOrdersRepository, IMapper mapper, IPatientCohortHelper patientCohortHelper)
        {
            _dnaOrdersRepository = dnaOrdersRepository;
            _mapper = mapper;
            _patientCohortHelper = patientCohortHelper;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.IsReplacedAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsReplacedAsync(int id)
        {
            return await _dnaOrdersRepository
                .All()
                .ByReplacedOrderId(id)
                .AnyAsync();
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetByBarcodeAsync"/>
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        public async Task<DnaOrder> GetByBarcodeAsync(string barcode)
        {
            var order = await _dnaOrdersRepository
                .All()
                .ByBarcode(barcode)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .FirstOrDefaultAsync();

            if (order is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Order with barcode: {barcode} does not exist");
            }
            
            return order;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.SelectForIntegrationAsync"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public async Task<DnaOrder[]> SelectForIntegrationAsync(
            DateTime from, 
            DateTime to, 
            OrderStatus[] statuses)
        {
            var orders = await _dnaOrdersRepository
                .All()
                .ByDate(from, to)
                .ByStatuses(statuses)
                .OrderBy(x => x.OrderedAt)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .AsNoTracking()
                .ToArrayAsync();

            return orders;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetReadyForPublishAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ReadyForPublishDnaOrderModel[]> GetReadyForPublishAsync()
        {
            var orders = await _dnaOrdersRepository
                .All()
                .ReadyForPublishing()
                .Select(x => new ReadyForPublishDnaOrderModel
                {
                    Id = x.Id!.Value,
                    FirstName = x.Patient.User.FirstName,
                    LastName = x.Patient.User.LastName,
                    StreetAddress = x.Patient.User.ShippingAddress.StreetAddress1 + ' ' + x.Patient.User.ShippingAddress.StreetAddress2,
                    City = x.Patient.User.ShippingAddress.City,
                    State = x.Patient.User.ShippingAddress.State,
                    ZipCode = x.Patient.User.ShippingAddress.ZipCode,
                    DateOfBirth = x.Patient.User.Birthday,
                    Gender = x.Patient.User.Gender
                }).ToArrayAsync();
        
            return orders;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<DnaOrder[]> GetAsync(int patientId)
        {
            var orders = await _dnaOrdersRepository
                .All()
                .RelatedToPatient(patientId)
                .OrderBy(x => x.OrderedAt)
                .IncludeReplacement()
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .AsNoTracking()
                .ToArrayAsync();

            return orders;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetByNumberAsync"/>
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task<DnaOrder> GetByNumberAsync(string number)
        {
            var order = await _dnaOrdersRepository
                .All()
                .ByNumber(number)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .FirstOrDefaultAsync();

            if (order is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Order with number: {number} does not exist.");
            }
            
            return order;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DnaOrder> GetByIdAsync(int id)
        {
            var order = await _dnaOrdersRepository
                .All()
                .ById(id)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .FirstOrDefaultAsync();

            if (order is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Order does not exist.", exceptionParam);
            }
            
            return order;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetActiveAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<DnaOrder[]> GetActiveAsync()
        {
            var orders = await _dnaOrdersRepository
                .All()
                .ExceptStatus(OrderStatus.Completed)
                .ExceptStatus(OrderStatus.Failed)
                .IncludeOrderItemsWithAddOns()
                .ToArrayAsync();

            return orders;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.CreateAsync"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<DnaOrder> CreateAsync(DnaOrder order)
        {
            await _dnaOrdersRepository.AddAsync(order);

            await _dnaOrdersRepository.SaveAsync();

            return order;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.UpdateAsync"/>
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<DnaOrder> UpdateAsync(DnaOrder order)
        {
            _dnaOrdersRepository.Edit(order);

            await _dnaOrdersRepository.SaveAsync();

            return order;
        }

        public async Task<DnaOrder> CreateAsync(ManualDnaOrder order)
        {
            await _dnaOrdersRepository.AddAsync(order);

            await _dnaOrdersRepository.SaveAsync();

            return order;
        }

        public async Task<DnaOrder> UpdateAsync(ManualDnaOrder order)
        {
            _dnaOrdersRepository.Edit(order);

            await _dnaOrdersRepository.SaveAsync();

            return order;
        }

        /// <summary>
        /// <see cref="IDnaOrdersService.GetByOrderStatus"/>
        /// <param name="practiceId"></param>
        /// <param name="status"></param>
        /// </summary>
        /// <returns></returns>
        public async Task<DnaOrder[]> GetByOrderStatus(int practiceId, OrderStatus status)
        {
            var orders = await _dnaOrdersRepository
                .All()
                .OrderByDescending(x => x.Id)
                .RelatedToPractice(practiceId)
                .ByStatus(status)
                .IncludeOrderItemsWithAddOns()
                .IncludePatient()
                .AsNoTracking()
                .ToArrayAsync();

            return orders;
        }

        public async Task<DnaOrderModel[]> GetByOrderStatusForDropship(int practiceId, OrderStatus status)
        {
            var models = await GetByOrderStatus(practiceId, status);
            
            var dnaOrders = _mapper.Map<DnaOrderModel[]>(models);

            return await DecorateShippingStatus(dnaOrders);
        }

        private async Task<DnaOrderModel[]> DecorateShippingStatus(DnaOrderModel[] models)
        {
            var results = new List<DnaOrderModel>();

            foreach (var model in models)
            {
                model.ShouldShipFirstClass = await _patientCohortHelper.IsPremiumPatient(model.PatientId);

                results.Add(model);
            }

            return results.ToArray();
        }
    }
}