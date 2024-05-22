using System.Linq;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Tags
{
    /// <summary>
    /// Manages patients
    /// </summary>
    public class TagsService : ITagsService
    {
        private readonly IGeneralRepository<Tag> _tagsRepository;

        public TagsService(IGeneralRepository<Tag> tagsRepository) 
        {
            _tagsRepository = tagsRepository;
        }
        
        /// <summary>
        /// Gets or creates a tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<Tag> GetOrCreate(Tag tag)
        {
            var result = await _tagsRepository
                .All()
                .Where(o => o.Name == tag.Name)
                .FirstOrDefaultAsync();

            if (result != null)
            {
                return result;
            }

            return await Create(tag);
        }

        public async Task<Tag?> Get(string tag)
        {
            return await _tagsRepository
                .All()
                .Where(o => o.Name == tag)
                .FirstOrDefaultAsync();
        }

        #region Private
        

        /// <summary>
        /// Creates a tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        private async Task<Tag> Create(Tag tag) {

            await _tagsRepository.AddAsync(tag);
            await _tagsRepository.SaveAsync();

            return tag;
        }
        
        
        #endregion
    }
}
