using MediatR;

namespace WildHealth.Application.Events.Orders
{
    public class LabOrderUnsolicitedEvent : INotification
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public string OrderNumber { get; }
        
        public LabOrderUnsolicitedEvent(
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