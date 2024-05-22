using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Products;
using WildHealth.Domain.Entities.Patients;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.PatientProducts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Products;

public class VoidProductCommandHandler : IRequestHandler<VoidProductCommand, PatientProduct>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IAppointmentsService _appointmentsService;
    private readonly IGeneralRepository<Appointment> _appointmentsRepository;
    private readonly ILogger _logger;

    public VoidProductCommandHandler(
        IPatientProductsService patientProductsService, 
        IAppointmentsService appointmentsService,
        IGeneralRepository<Appointment> appointmentsRepository,
        ILogger<VoidProductCommandHandler> logger)
    {
        _patientProductsService = patientProductsService;
        _appointmentsService = appointmentsService;
        _appointmentsRepository = appointmentsRepository;
        _logger = logger;
    }
    
    public async Task<PatientProduct> Handle(VoidProductCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started un using patient product with [Id] = {command.Id}");

        var product = await _patientProductsService.GetAsync(command.Id);

        if (product.CanReuseProduct())
        {
            product = await UnUseProductAsync(product);
        }
        else
        {
            product = await DeleteProductAsync(product);
        }

        _logger.LogInformation($"Finished un using patient product with [Id] = {command.Id}");

        return product;
    }
    
    #region private

    private async Task<PatientProduct> UnUseProductAsync(PatientProduct product)
    {
        product.UnUseProduct();

        await _patientProductsService.UpdateAsync(product);

        var appointment = await _appointmentsRepository
            .All()
            .Where(o => o.ProductId == product.GetId())
            .FirstOrDefaultAsync();

        if (appointment is null)
        {
            return product;
        }

        appointment.ProductId = null;

        await _appointmentsService.EditAppointmentAsync(appointment);

        return product;
    }
    
    private async Task<PatientProduct> DeleteProductAsync(PatientProduct product)
    {
        if (product.CanDeleteProduct())
        {
            await _patientProductsService.DeleteAsync(product);
        }
        
        return product;
    }

    #endregion
}