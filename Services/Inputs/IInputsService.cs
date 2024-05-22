using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Enums.Inputs;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// Provides methods for working with patient inputs
    /// </summary>
    public interface IInputsService
    {
        /// <summary>
        /// Returns inputs aggregator by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<InputsAggregator> GetAggregatorAsync(int patientId, FileInputType type);
        
        /// <summary>
        /// Returns inputs aggregator by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="includeInsights"></param>
        /// <returns></returns>
        Task<InputsAggregator> GetAggregatorAsync(int patientId, bool includeInsights = false);

        /// <summary>
        /// Fill out inputs
        /// </summary>
        /// <param name="aggregator"></param>
        /// <param name="input"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task FillOutInputsAsync(InputsAggregator aggregator, FileInput input, byte[] content);
        
        #region file inputs 
        
        /// <summary>
        /// Returns file input
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<FileInput> GetFileInputAsync(int id, int patientId);
        
        /// <summary>
        /// Returns all file inputs
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<FileInput>> GetFileInputsAsync(int patientId);

        /// <summary>
        /// Creates file input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<FileInput> CreateFileInputAsync(FileInput input);

        /// <summary>
        /// Update file input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<FileInput> UpdateFileInputAsync(FileInput input);

        /// <summary>
        /// Deletes file input
        /// </summary>
        /// <param name="fileInput"></param>
        /// <returns></returns>
        Task<FileInput> DeleteFileInputAsync(FileInput fileInput);

        #endregion

        #region lab inputs 
        
        /// <summary>
        /// Returns lab inputs.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<LabInput>> GetLabInputsAsync(int patientId);

        /// <summary>
        /// Returns lab inputs for integration.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<LabInput>> GetLabInputsIntegrationAsync(int patientId);
        
        /// <summary>
        /// Updates lab inputs.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<LabInput>> UpdateLabInputsAsync(UpdateLabInputsModel model, int patientId);
        
        /// <summary>
        /// Updates lab inputs.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        Task<ICollection<LabInput>> DeleteLabValues(int patientId, string datasetId);
        
        /// <summary>
        /// Updates a single lab input
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<LabInput> UpdateLabInputAsync(LabInput model);

        #endregion
        
        #region general inputs
        
        /// <summary>
        /// Returns general inputs.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<GeneralInputs> GetGeneralInputsAsync(int patientId);

        /// <summary>
        /// Returns lab inputs that do not reference an alias
        /// </summary>
        /// <returns></returns>
        Task<ICollection<LabInput>> GetLabInputsWithoutAliasReference();

        /// <summary>
        /// Updates general inputs.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<GeneralInputs> UpdateGeneralInputsAsync(GeneralInputsModel model, int patientId);

        /// <summary>
        /// Updates general inputs.
        /// </summary>
        /// <param name="generalInputs"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<GeneralInputs> UpdateGeneralInputsAsync(GeneralInputs generalInputs, int patientId);

        /// <summary>
        /// Update HideApoe status for patient
        /// </summary>
        /// <param name="hideApoe"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task UpdateHideApoe(YesNo hideApoe, int patientId);

        /// <summary>
        /// Get HideApoe status for patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<YesNo> GetHideApoe(int patientId);
        

        #endregion 
        
        #region microbiome inputs
        
        /// <summary>
        /// Returns microbiome inputs.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<MicrobiomeInput>> GetMicrobiomeInputsAsync(int patientId);
        
        /// <summary>
        /// Updates microbiome inputs.
        /// </summary>
        /// <param name="models"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<ICollection<MicrobiomeInput>> UpdateMicrobiomeInputsAsync(ICollection<MicrobiomeInputModel> models, int patientId);
        
        /// <summary>
        /// Updates microbiome inputs.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        Task<ICollection<MicrobiomeInput>> UpdateMicrobiomeInputsAsync(ICollection<MicrobiomeInput> inputs);

        #endregion
    }
}