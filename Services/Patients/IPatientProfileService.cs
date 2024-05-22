using System;
using System.Threading.Tasks;

namespace WildHealth.Application.Services.Patients;

public interface IPatientProfileService
{
    Task<string> GetProfileLink(int patientId, int practiceId);
    Task<string> GetDashboardLink(int practiceId);
    Task<MembershipInfo> GetMembershipInfo(int patientId, DateTime now);
}