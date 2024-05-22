using MediatR;

namespace WildHealth.Application.Events.Orders
{
    public class LabOrderCompletedEvent : INotification
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public string OrderNumber { get; }
        
        public LabOrderCompletedEvent(
            int patientId, 
            int reportId, 
            string orderNumber)
        {
            PatientId = patientId;
            ReportId = reportId;
            OrderNumber = orderNumber;
        }
    }
}