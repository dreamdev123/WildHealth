using MediatR;

namespace WildHealth.Application.Events.Orders
{
    public class LabOrderPlacedEvent : INotification
    {
        public int PatientId { get; }
        
        public int ReportId { get; }
        
        public string OrderNumber { get; }
        
        public string[] TestCodes { get; }
        
        public LabOrderPlacedEvent(
            int patientId, 
            int reportId, 
            string orderNumber, 
            string[] testCodes)
        {
            PatientId = patientId;
            ReportId = reportId;
            OrderNumber = orderNumber;
            TestCodes = testCodes;
        }
    }
}