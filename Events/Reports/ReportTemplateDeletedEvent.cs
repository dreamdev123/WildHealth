using MediatR;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;

namespace WildHealth.Application.Events.Reports;

public record ReportTemplateDeletedEvent(ReportTemplate ReportTemplate) : INotification;