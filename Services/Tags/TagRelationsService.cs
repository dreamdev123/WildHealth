
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Tags;
using WildHealth.Domain.Interfaces;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Tags
{
    /// <summary>
    /// Manages patients
    /// </summary>
    public class TagRelationsService : ITagRelationsService
    {
        private readonly IGeneralRepository<TagRelation> _tagRelationsRepository;
        private readonly ITagsService _tagsService;

        public TagRelationsService(
            IGeneralRepository<TagRelation> tagRelationsRepository,
            ITagsService tagsService
        ) {
            _tagRelationsRepository = tagRelationsRepository;
            _tagsService = tagsService;
        }

        public async Task<TagRelation> GetOrCreate(ITaggable taggable, string name) {
            var t = new Tag(
                name: name,
                description: name
            );

            var tag = await _tagsService.GetOrCreate(t);

            return await GetOrCreate(new TagRelation(
                tagId: tag.GetId(),
                uniqueGuid: taggable.UniversalId
            ));
        }
    

        public async Task<TagRelation> GetOrCreate(TagRelation tagRelation)
        {
            var result = await _tagRelationsRepository
                .All()
                .Where(o => o.TagId == tagRelation.TagId && o.UniqueGuid == tagRelation.UniqueGuid)
                .FirstOrDefaultAsync();

            if (result is not null)
            {
                return result;
            }

            return await Create(tagRelation);
        }       


        public async Task<TagRelation?> Get(ITaggable entity, string tagName)
        {
            var tag = await _tagsService.Get(tagName);

            if (tag is null)
            {
                return null;
            }
            
            var relation = _tagRelationsRepository
                .All()
                .FirstOrDefault(o => o.Tag.Id == tag.Id && o.UniqueGuid == entity.UniversalId);

            return relation;

        }

        public async Task<IEnumerable<TagRelation>> GetAllOfTag(string tagName)
        {
            var tag = await _tagsService.Get(tagName);

            if (tag is null)
            {
                // tag still not present
                return Enumerable.Empty<TagRelation>();
            }

            return _tagRelationsRepository.All()
                .Where(o => o.Tag.Id == tag.Id);

        }

        /// <summary>
        /// <see cref="ITagRelationsService.GetAllOfEntity"/>
        /// </summary>
        /// <param name="entityUniversalId"></param>
        /// <returns></returns>
        public async Task<TagRelation[]> GetAllOfEntity(Guid entityUniversalId)
        {
            return await _tagRelationsRepository.All()
                .Where(x => x.UniqueGuid == entityUniversalId)
                .Include(x => x.Tag)
                .ToArrayAsync();
        }

        public async Task Delete(TagRelation tagRelation)
        {
            _tagRelationsRepository.Delete(tagRelation);
            await _tagRelationsRepository.SaveAsync();
        }
        
        #region private
        
        /// <summary>
        /// Creates a tag relation
        /// </summary>
        /// <param name="tagRelation"></param>
        /// <returns></returns>
        private async Task<TagRelation> Create(TagRelation tagRelation) {

            await _tagRelationsRepository.AddAsync(tagRelation);
            await _tagRelationsRepository.SaveAsync();

            return tagRelation;
        }
        
        #endregion
    }
}
