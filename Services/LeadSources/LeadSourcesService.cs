
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.LeadSources;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.Services.LeadSources
{
    public class LeadSourcesService: ILeadSourcesService
    {
        private readonly IGeneralRepository<LeadSource> _leadSourcesRepository;
        private readonly IGeneralRepository<PatientLeadSource> _patientLeadSourcesRepository;
        private readonly IAuthTicket _authTicket;

        public LeadSourcesService(
            IGeneralRepository<LeadSource> leadSourcesRepository,
            IGeneralRepository<PatientLeadSource> patientLeadSourcesRepository,
            IAuthTicket authTicket)
        {
            _leadSourcesRepository = leadSourcesRepository;
            _patientLeadSourcesRepository = patientLeadSourcesRepository;
            _authTicket = authTicket;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.GetAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<LeadSource> GetAsync(int id, int practiceId)
        {
            var leadSource = await _leadSourcesRepository
                .All()
                .ById(id)
                .RelatedToPractice(practiceId)
                .FirstOrDefaultAsync();

            if (leadSource is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Lead source does not exist.", exceptionParam);
            }

            return leadSource;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.GetAllAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<List<LeadSource>> GetAllAsync(int practiceId)
        {
            var leadSources = await _leadSourcesRepository
                .All()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .IncludePatients()
                .AsNoTracking()
                .ToListAsync();

            return leadSources;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.GetActiveAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<List<LeadSource>> GetActiveAsync(int practiceId)
        {
            var leadSources = await _leadSourcesRepository
                .All()
                .Active()
                .NotDeleted()
                .RelatedToPractice(practiceId)
                .AsNoTracking()
                .ToListAsync();

            return leadSources;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.CreateAsync"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isOther"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<LeadSource> CreateAsync(string name, bool isOther, int practiceId)
        {
            var leadSource = await _leadSourcesRepository
                .All()
                .NotDeleted()
                .RelatedToPractice(_authTicket.GetPracticeId())
                .FirstOrDefaultAsync(x => x.Name == name);

            if (leadSource != null)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"Lead source with name: {name} already exists.");
            }

            leadSource = new LeadSource(
                name: name, 
                isOther: isOther,
                practiceId: practiceId);

            await _leadSourcesRepository.AddAsync(leadSource);

            await _leadSourcesRepository.SaveAsync();

            return leadSource;
        }


        /// <summary>
        /// <see cref="ILeadSourcesService.ChangeActivityAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<LeadSource> ChangeActivityAsync(int id)
        {
            var leadSource = await GetAsync(id, _authTicket.GetPracticeId());

            leadSource.ChangeActivity();

            _leadSourcesRepository.Edit(leadSource);

            await _leadSourcesRepository.SaveAsync();
            
            return leadSource;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.DeleteAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<LeadSource> DeleteAsync(int id)
        {
            var leadSource = await GetAsync(id,  _authTicket.GetPracticeId());

            if (leadSource is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Unable to delete. Lead source does not exist.", exceptionParam);
            }

            if (leadSource.IsOther)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Unable to delete default lead source.");
            }

            _leadSourcesRepository.Delete(leadSource);

            await _leadSourcesRepository.SaveAsync();

            return leadSource;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.CreatePatientLeadSourceAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="leadSource"></param>
        /// <param name="otherSource"></param>
        /// <param name="podcastSource"></param>
        /// <returns></returns>
        public async Task<PatientLeadSource> CreatePatientLeadSourceAsync(Patient patient, LeadSource leadSource, string? otherSource = null, string? podcastSource = null)
        {
            var patientLeadSource = new PatientLeadSource(leadSource, patient, otherSource, podcastSource);

            if (leadSource.IsOther && otherSource is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Other lead source should be provided.");
            }

            await _patientLeadSourcesRepository.AddAsync(patientLeadSource);

            await _patientLeadSourcesRepository.SaveAsync();

            return patientLeadSource;
        }

        /// <summary>
        /// <see cref="ILeadSourcesService.DeletePatientLeadSourceAsync"/>
        /// </summary>
        /// <param name="patientLeadSource"></param>
        /// <returns></returns>
        public async Task<PatientLeadSource> DeletePatientLeadSourceAsync(PatientLeadSource patientLeadSource)
        {
            _patientLeadSourcesRepository.Delete(patientLeadSource);
            await _patientLeadSourcesRepository.SaveAsync();

            return patientLeadSource;
        }
    }
}
