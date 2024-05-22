using WildHealth.Application.Services.Reports.Base;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Utils.Reports;

public static class ReportServiceRegistrationHelper
{
    public delegate ReportServiceBase ServiceResolver(ReportType type);
}