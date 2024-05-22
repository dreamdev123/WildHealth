using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Comments;

namespace WildHealth.Application.Services.Comments;

public interface ICommentsService
{
    Task<Comment> CreateAsync(Comment comment);
    
    Task<Comment> EditAsync(int id, string description);

    Task<Comment[]> GetByCommentableUniversalId(Guid commentableUniversalId);

    Task MarkAsDeletedAsync(int id);
}