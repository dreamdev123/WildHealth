using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Comments;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Comments;

public class CommentsService : ICommentsService
{
    private readonly IGeneralRepository<Comment> _commentsRepository;

    public CommentsService(IGeneralRepository<Comment> commentsRepository)
    {
        _commentsRepository = commentsRepository;
    }

    public async Task<Comment> CreateAsync(Comment comment)
    {
        await _commentsRepository.AddAsync(comment);

        await _commentsRepository.SaveAsync();

        return comment;
    }

    public async Task<Comment> EditAsync(int id, string description)
    {
        var comment = await _commentsRepository
            .All()
            .ById(id)
            .Where(o => !o.IsDeleted)
            .FindAsync();

        comment.Description = description;
        
        _commentsRepository.Edit(comment);

        await _commentsRepository.SaveAsync();

        return comment;
    }

    public async Task<Comment[]> GetByCommentableUniversalId(Guid commentableUniversalId)
    {
        var comments = await _commentsRepository
            .All()
            .Where(o => o.CommentableUniversalId == commentableUniversalId && !o.IsDeleted)
            .ToArrayAsync();

        return comments;
    }

    public async Task MarkAsDeletedAsync(int id)
    {
        var comment = await _commentsRepository.All().ById(id).FindAsync();

        comment.IsDeleted = true;
        
        _commentsRepository.Edit(comment);

        await _commentsRepository.SaveAsync();
    }

}