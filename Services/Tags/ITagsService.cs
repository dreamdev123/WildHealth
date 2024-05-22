using System.Threading.Tasks;
using WildHealth.Domain.Entities.Tags;

namespace WildHealth.Application.Services.Tags
{
    /// <summary>
    /// Manages tag
    /// </summary>
    public interface ITagsService
    {
        /// <summary>
        /// Gets or creates a tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<Tag> GetOrCreate(Tag tag);
        
        
        
        /// <summary>
        /// Get a tag or null
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>Tag</returns>
        Task<Tag?> Get(string tag);
        

        
    }
}
