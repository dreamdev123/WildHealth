using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Application.Services.Attachments;
using WildHealth.Domain.Enums.Attachments;
using MediatR;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetInsuranceUploadsCommandHandler : IRequestHandler<GetInsuranceUploadsCommand, Attachment[]>
{
    private static readonly AttachmentType[] InsuranceAttachmentTypes = 
    {
        AttachmentType.InsuranceCardFront,
        AttachmentType.InsuranceCardBack
    };
    
    private readonly IAttachmentsService _attachmentsService;
    private readonly IPatientsService _patientsService;
    private readonly IUsersService _usersService;
    private readonly IPermissionsGuard _permissionsGuard;

    public GetInsuranceUploadsCommandHandler(
        IAttachmentsService attachmentsService, 
        IPatientsService patientsService, 
        IUsersService usersService,
        IPermissionsGuard permissionsGuard)
    {
        _attachmentsService = attachmentsService;
        _patientsService = patientsService;
        _usersService = usersService;
        _permissionsGuard = permissionsGuard;
    }

    public async Task<Attachment[]> Handle(GetInsuranceUploadsCommand command, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(command);
        
        _permissionsGuard.AssertPermissions(user);
        
        var attachments = await _attachmentsService.GetUserAttachmentsByTypesAsync(
            userId: user.GetId(),
            attachmentTypes: InsuranceAttachmentTypes
        );

        return attachments;
    }
    
    #region private
    
    /// <summary>
    /// Fetches and returns user 
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="AppException"></exception>
    private async Task<User> GetUserAsync(GetInsuranceUploadsCommand command)
    {
        if (command.UserId.HasValue)
        {
            var specification = UserSpecifications.UserWithIntegrations;

            return await _usersService.GetAsync(command.UserId.Value, specification);
        }

        if (command.PatientId.HasValue)
        {
            var specification = PatientSpecifications.PatientWithIntegrations;

            var patient = await _patientsService.GetByIdAsync(command.PatientId.Value, specification);

            return patient.User;
        }

        throw new AppException(HttpStatusCode.BadRequest, "Patient is should be Greater Than than 0");
    }
    
    #endregion
}