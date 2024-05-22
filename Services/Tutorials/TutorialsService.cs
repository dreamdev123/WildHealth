using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Tutorials;
using WildHealth.Common.Models.Tutorials;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Application.Extensions.Query;

namespace WildHealth.Application.Services.Tutorials
{
    /// <summary>
    /// <see cref="ITutorialsService"/>
    /// </summary>
    public class TutorialsService : ITutorialsService
    {
        private readonly IGeneralRepository<User> _usersRepository;
        private readonly IGeneralRepository<TutorialStatus> _tutorialStatusRepository;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientService;
        private readonly ILogger<TutorialsService> _logger;

        public TutorialsService(
            IGeneralRepository<User> usersRepository,
            IGeneralRepository<TutorialStatus> tutorialStatusRepository,
            IEmployeeService employeeService,
            IPatientsService patientService,
            ILogger<TutorialsService> logger)
        {
            _usersRepository = usersRepository;
            _tutorialStatusRepository = tutorialStatusRepository;
            _employeeService = employeeService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="ITutorialsService.GetTutorialStatusByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TutorialStatus> GetTutorialStatusByIdAsync(int id)
        {
            var tutorialStatus = await _tutorialStatusRepository
                .All()
                .ById(id)
                .FindAsync();

            return tutorialStatus;
        }

        /// <summary>
        /// <see cref="ITutorialsService.CheckFirstView"/>
        /// </summary>
        /// <param name="tutorialName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<TutorialStatus> CheckFirstView(string tutorialName, int userId)
        {
            var user = await _usersRepository
                .All()
                .Include(o => o.Patient)
                .Include(o => o.Employee)
                .Where(o => o.Id == userId)
                .FindAsync();

            var tutorialStatus = await _tutorialStatusRepository
                .All()
                .Where(o => o.UserId == userId && o.TutorialName == tutorialName)
                .FirstOrDefaultAsync();

            if (tutorialStatus is null)
            {
                tutorialStatus = new TutorialStatus(
                tutorialName: tutorialName,
                userId: userId,
                isAcknowledged: false);

                await _tutorialStatusRepository.AddAsync(tutorialStatus);

                await _tutorialStatusRepository.SaveAsync();
            }

            return tutorialStatus;
        }

        /// <summary>
        /// Acknowledge the TutorialStatus for the given user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TutorialStatus> AcknowledgeTutorialStatus(int id)
        {
            var tutorialStatus = await GetTutorialStatusByIdAsync(id);

            tutorialStatus.IsAcknowledged = true;
            
            _tutorialStatusRepository.Edit(tutorialStatus);

            await _tutorialStatusRepository.SaveAsync();

            return tutorialStatus;
        }

        /// <summary>
        /// <see cref="ITutorialsService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TutorialStatus> DeleteAsync(int id)
        {
            var tutorialStatus = await GetTutorialStatusByIdAsync(id);

            _tutorialStatusRepository.Delete(tutorialStatus);

            await _tutorialStatusRepository.SaveAsync();

            return tutorialStatus;
        }

        /// <summary>
        /// <see cref="ITutorialsService.UpdateAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<TutorialStatus> UpdateAsync(TutorialStatusModel model)
        {
            var tutorialStatus = await GetTutorialStatusByIdAsync(model.Id);
               
            tutorialStatus.TutorialName = model.TutorialName;
            tutorialStatus.IsAcknowledged = model.IsAcknowledged;

            _tutorialStatusRepository.Edit(tutorialStatus);

            await _tutorialStatusRepository.SaveAsync();

            return tutorialStatus;
        }
    }
}
