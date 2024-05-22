using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Tags;
using WildHealth.Domain.Interfaces;

namespace WildHealth.Application.Services.Tags
{
    /// <summary>
    /// Manages tag relation
    /// </summary>
    public interface ITagRelationsService
    {
        /// <summary>
        /// Gets or creates a tag relation
        /// </summary>
        /// <param name="tagRelation"></param>
        /// <returns></returns>
        Task<TagRelation> GetOrCreate(TagRelation tagRelation);

        /// <summary>
        /// Gets or creates a tag relation for a patient
        /// </summary>
        /// <param name="taggable"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<TagRelation> GetOrCreate(ITaggable taggable, string name);

        /// <summary>
        ///  Get a tag or null
        /// </summary>
        /// <param name="taggableEntity"></param>
        /// <param name="tagName"></param>
        /// <returns>Tag</returns>
        Task<TagRelation?> Get(ITaggable taggableEntity, string tagName);
        
        /// <summary>
        /// Returns all tag relations
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        Task<IEnumerable<TagRelation>> GetAllOfTag(string tagName);

        /// <summary>
        /// Get all tags of an entity
        /// </summary>
        /// <param name="entityUniversalId"></param>
        /// <returns></returns>
        Task<TagRelation[]> GetAllOfEntity(Guid entityUniversalId);

        /// <summary>
        /// Removes tag relation
        /// </summary>
        /// <param name="tagRelation"></param>
        /// <returns></returns>
        Task Delete(TagRelation tagRelation);
    }
}
