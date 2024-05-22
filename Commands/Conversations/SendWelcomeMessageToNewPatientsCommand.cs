using MediatR;

namespace WildHealth.Application.Commands.Conversations
{
    /// <summary>
    /// Command Send an initial Welcome Message when:
    ///    Patient hasn't send any message 24 hs since checkout    
    ///    And Patient get 
    /// </summary>
    public class SendWelcomeMessageToNewPatientsCommand : IRequest
    {
        public string Message { get; }
            
        public SendWelcomeMessageToNewPatientsCommand() { 

            Message = @"Welcome to Wild Health {{patient_first_name}}! 
                        
                        I'm your Health Coach {{coach_first_name}} and I can't wait to meet you soon - you'll be able to schedule your first visit with me upon completing your health forms. In the meantime, I wanted to tell you about how to use messaging. 
                        
                        You can message me here anytime with health questions or if you need anything like prescriptions, supplement recommendations, labs and more. You can also message your care coordinator through the support chat with any administrative questions like billing, scheduling, tech support, or any general questions. You can also download our mobile app if you want easy access to messaging ([Apple Store](https://apps.apple.com/us/app/wild-health/id1561666113) / [Play Store](https://play.google.com/store/apps/details?id=com.wildhealth)).
                        
                        While we try to reply quickly, if something is an emergency, please call 911. 
                        
                        Please note, our typical response time is within 24 business hours although some inquiries may require extra time. 
                        
                        If you have any questions while you're getting started, reach out to your care coordinator, and I look forward to meeting soon!";
        }
    }
}
