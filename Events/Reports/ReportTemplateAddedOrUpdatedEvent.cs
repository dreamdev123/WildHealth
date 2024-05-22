using MediatR;

namespace WildHealth.Application.Events.Reports;

public record ReportTemplateAddedOrUpdatedEvent(int ReportTemplateId) : INotification;