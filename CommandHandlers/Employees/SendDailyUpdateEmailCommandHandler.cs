using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.HealthReports;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.EmailFactory.Models.DailyUpdateEmail;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Emails;
using WildHealth.Common.Models.Appointments;
using WildHealth.Common.Models.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Settings;
using MediatR;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.CommandHandlers.Employees;

public class SendDailyUpdateEmailCommandHandler : IRequestHandler<SendDailyUpdateEmailCommand>
{
    private const string Subject = "Your {0} update!";
    
    private static readonly int[] RoleIds =
    {
        Roles.LocationDirectorId,
        Roles.ProviderId,
        Roles.CoachId
    };
    
    private static readonly string[] SettingKeys =
    {
        SettingsNames.General.ApplicationBaseUrl,
        SettingsNames.Emails.HeaderUrl,
        SettingsNames.Emails.LogoUrl,
        SettingsNames.Emails.WhiteLogoUrl,
        SettingsNames.Emails.WHLinkLogoUrl,
        SettingsNames.Emails.InstagramUrl,
        SettingsNames.Emails.WHInstagramLogoUrl
    };

    private readonly IPracticeService _practiceService;
    private readonly IEmployeeService _employeeService;
    private readonly IPatientsService _patientsService;
    private readonly IHealthReportService _healthReportService;
    private readonly ISettingsManager _settingsManager;
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    private readonly DateTime _currentUtcDate;
    private readonly AppOptions _appOptions;
    private readonly ILogger _logger;

    public SendDailyUpdateEmailCommandHandler(
        IPracticeService practiceService,
        IEmployeeService employeeService, 
        IPatientsService patientsService,
        IHealthReportService healthReportService,
        IDateTimeProvider dateTimeProvider, 
        ISettingsManager settingsManager,
        IEmailService emailService,
        IEmailFactory emailFactory,
        IOptions<AppOptions> appOptions,
        ILogger<SendDailyUpdateEmailCommandHandler> logger)
    {
        _practiceService = practiceService;
        _employeeService = employeeService;
        _patientsService = patientsService;
        _settingsManager = settingsManager;
        _emailService = emailService;
        _emailFactory = emailFactory;
        _healthReportService = healthReportService;
        _currentUtcDate = dateTimeProvider.UtcNow();
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task Handle(SendDailyUpdateEmailCommand request, CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetByRolesIdsAsync(RoleIds);

        var employeeGroups = employees.GroupBy(x => x.User.PracticeId);
        
        foreach (var group in employeeGroups)
        {
            var settings = await _settingsManager.GetSettings(SettingKeys, group.Key);

            var practice = await _practiceService.GetOriginalPractice(group.Key);

            var practiceDomain = await _practiceService.GetAsync(group.Key);

            // If the practice is not active, then we no longer want to send any communication
            if (!practiceDomain.IsActive)
            {
                continue;
            }
            
            foreach (var employee in group)
            {
                try
                {
                    var email = employee.RoleId switch
                    {
                        Roles.LocationDirectorId => await BuildCareCoordinatorEmailAsync(employee, practice, settings),
                        Roles.ProviderId => await BuildProviderEmailAsync(employee, practice, settings),
                        Roles.CoachId => await BuildHealthCoachEmailAsync(employee, practice, settings),
                        _ => throw new ArgumentException("Unsupported employee role.")
                    };

                    var subject = string.Format(Subject, _currentUtcDate.DayOfWeek.ToString());

                    await _emailService.SendAsync(employee.User.Email, subject, email.Html, employee.User.PracticeId);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error during sending daily updates for employee with [Id] = {employee.Id}: {e.Message}", e);
                }
            }
        }
    }

    #region private
    
    private async Task<EmailDataResult> BuildCareCoordinatorEmailAsync(
        Employee employee, 
        PracticeModel practice,
        IDictionary<string, string> settings)
    {
        var employeeData = await _employeeService.GetEmployeeDashboardModel(employee.GetId());
        
        var data = new CareCoordinatorDailyUpdateEmailModel
        {
            FirstName = employee.User.FirstName,
            DayOfWeek = _currentUtcDate.DayOfWeek.ToString(),
            TotalUnreadMessages = employeeData.TotalUnreadMessages
        };
        
        data = FillOutGeneralInformation(data, practice, settings);
        
        return await _emailFactory.Create(new EmailDataModel<CareCoordinatorDailyUpdateEmailModel>
        {
            Data = data
        });
    }
    
    private async Task<EmailDataResult> BuildHealthCoachEmailAsync(
        Employee employee, 
        PracticeModel practice,
        IDictionary<string, string> settings)
    {
        var employeeData = await _employeeService.GetEmployeeDashboardModel(employee.GetId());
        var reports = await GetUnpublishedHealthReports(
            employeeId: employee.GetId(),
            settings: settings
        );
        
        var data = new HealthCoachDailyUpdateEmailModel
        {
            UnpublishedHealthReports = reports,
            FirstName = employee.User.FirstName,
            TotalUnpublishedHealthReports = reports.Length,
            DayOfWeek = _currentUtcDate.DayOfWeek.ToString(),
            TotalUnreadMessages = employeeData.TotalUnreadMessages,
            UpcomingAppointments = employeeData.UpcomingAppointments
                .Where(x => x.WithType != AppointmentWithType.Other)
                .Select(x => ToAppointmentModel(x, settings)),
            RecentlyAssignedPatients = employeeData.RecentlyAssignedPatients.Select(x => ToPatientModel(x, settings)),
        };
        
        data = FillOutGeneralInformation(data, practice, settings);

        return await _emailFactory.Create(new EmailDataModel<HealthCoachDailyUpdateEmailModel>
        {
            Data = data
        });
    }
    
    private async Task<EmailDataResult> BuildProviderEmailAsync(
        Employee employee, 
        PracticeModel practice,
        IDictionary<string, string> settings)
    {
        var employeeData = await _employeeService.GetEmployeeDashboardModel(employee.GetId());
        var reports = await GetUnsignedHealthReports(
            employeeId: employee.GetId(),
            settings: settings
        );
        
        var data = new ProviderDailyUpdateEmailModel
        {
            UnsignedHealthReports = reports,
            FirstName = employee.User.FirstName,
            TotalUnsignedHealthReports = reports.Length,
            DayOfWeek = _currentUtcDate.DayOfWeek.ToString(),
            TotalUnreadMessages = employeeData.TotalUnreadMessages,
            UpcomingAppointments = employeeData.UpcomingAppointments
                .Where(x => x.WithType != AppointmentWithType.Other)
                .Select(x => ToAppointmentModel(x, settings)),
            RecentlyAssignedPatients = employeeData.RecentlyAssignedPatients.Select(x => ToPatientModel(x, settings)),
        };

        data = FillOutGeneralInformation(data, practice, settings);
        
        return await _emailFactory.Create(new EmailDataModel<ProviderDailyUpdateEmailModel>
        {
            Data = data
        });
    }

    private T FillOutGeneralInformation<T>(T data, PracticeModel practice, IDictionary<string, string> settings) where T: EmailData
    {
        data.PracticeId = practice.Id;
        data.PracticeName = practice.Name;
        data.PracticeEmail = practice.Email;
        data.ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        data.HeaderUrl = settings[SettingsNames.Emails.HeaderUrl];
        data.LogoUrl = settings[SettingsNames.Emails.LogoUrl];
        data.FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl];
        data.WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl];
        data.WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl];
        data.InstagramUrl = settings[SettingsNames.Emails.InstagramUrl];
        data.PracticePhoneNumber = practice.PhoneNumber;
        data.PracticeAddress = $"{practice.Address.Address1} " +
                               $"{practice.Address.City} " +
                               $"{practice.Address.State} " +
                               $"{practice.Address.ZipCode}";

        return data;
    }
    
    private async Task<HealthReportModel[]> GetUnsignedHealthReports(int employeeId, IDictionary<string, string> settings)
    {
        var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        
        var (reports, _) = await _healthReportService.GetReportsReviewingByAsync(employeeId, null, null);

        return reports.Select(x => new HealthReportModel
        {
            DateRequested = x.Status.Date,
            Patient = new PatientModel
            {
                FirstName = x.Patient.User.FirstName,
                LastName = x.Patient.User.LastName,
                Url = string.Format(_appOptions.PatientProfileUrl, appUrl, x.PatientId)
            }
        }).ToArray();
    }
    
    private async Task<HealthReportModel[]> GetUnpublishedHealthReports(int employeeId, IDictionary<string, string> settings)
    {
        var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        
        var (reports, _) = await _healthReportService.GetReportsCompletedByAsync(employeeId, null, null);

        return reports.Select(x => new HealthReportModel
        {
            DateRequested = x.Status.Date,
            Patient = new PatientModel
            {
                FirstName = x.Patient.User.FirstName,
                LastName = x.Patient.User.LastName,
                Url = string.Format(_appOptions.PatientProfileUrl, appUrl, x.PatientId)
            }
        }).ToArray();
    }

    private PatientModel ToPatientModel(EmployeeDashboardPatientModel patient, IDictionary<string, string> settings)
    {
        var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];

        return new PatientModel
        {
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Url = string.Format(_appOptions.PatientProfileUrl, appUrl, patient.PatientId)
        };
    }

    private AppointmentModel ToAppointmentModel(EmployeeAppointmentModel appointment, IDictionary<string, string> settings)
    {
        var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        
        return new AppointmentModel
        {
            Time = appointment.TimeZoneStartDate.ToString("hh:mm tt MM:dd"),
            Type = appointment.Purpose.ToString(),
            Patient = new PatientModel
            {
                FirstName = appointment.Patient.FirstName,
                LastName = appointment.Patient.LastName,
                Url = string.Format(_appOptions.PatientProfileUrl, appUrl, appointment.Patient.Id)
            }
        };
    }

    #endregion
}