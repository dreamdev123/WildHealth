using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Specifications;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Domain.Models.Patient;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class GetPatientWithVendorProfileCommandHandler : IRequestHandler<GetPatientWithVendorProfileCommand, PatientModel>
    {
        private readonly IMapper _mapper;
        private readonly IPatientsService _patientsService;
        private readonly ISpecificationProvider _specificationProvider;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IPermissionsGuard _permissionGuard;
        private readonly ILogger _logger;
        private readonly IAttachmentsService _attachmentsService;
        private readonly IMediator _mediator;
        
        public GetPatientWithVendorProfileCommandHandler(
            IMapper mapper,
            IPatientsService patientsService,
            IIntegrationServiceFactory integrationServiceFactory,
            ISpecificationProvider specificationProvider,
            IPermissionsGuard permissionGuard,
            ILogger<GetPatientWithVendorProfileCommandHandler> logger,
            IAttachmentsService attachmentsService,
            IMediator mediator)
        {
            _mapper = mapper;
            _patientsService = patientsService;
            _specificationProvider = specificationProvider;
            _integrationServiceFactory = integrationServiceFactory;
            _permissionGuard = permissionGuard;
            _logger = logger;
            _attachmentsService = attachmentsService;
            _mediator = mediator;
        }
        
        public async Task<PatientModel> Handle(GetPatientWithVendorProfileCommand command, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Getting patient with vendor details for [Id]: {command.PatientId}");
                
                var patient = await _patientsService.GetByIdAsync(
                    id: command.PatientId, 
                    specification: _specificationProvider.Get<Patient>(command.Specs)
                );
                
                _permissionGuard.AssertPermissions(patient);

                var patientModel = _mapper.Map<PatientModel>(patient);
                var patientDomain = PatientDomain.Create(patient);
                var integrationService = await _integrationServiceFactory.CreateAsync(patientDomain.CurrentPractice!.GetId());

                var link = integrationService.GetPaymentLinkForPatient(patient);
                
                patientModel.LinkToPaymentVendor = link.Url;
                
                // Get avatar photos for assigned staff
                foreach (var employee in patientModel.AssignedEmployees)
                {
                    var photoUrl = await _mediator.Send(new GetEmployeePhotoUrlCommand(employee.UserId));

                    employee.PhotoUrl = photoUrl;
                }
                
                _logger.LogInformation($"Finished getting patient with vendor details for [Id]: {command.PatientId}");
                
                return patientModel;
                
            }
            catch (AppException error)
            {
                // re-throw assertion guard if apply
                throw new AppException(HttpStatusCode.Forbidden, error.Message);
            }
        }
    }
}