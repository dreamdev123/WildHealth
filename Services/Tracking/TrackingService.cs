using System;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Common.Models.Tracking;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Tracking;

public class TrackingService : ITrackingService
{
    private readonly IGeneralRepository<Claim> _claimsRepository;
    private readonly IGeneralRepository<SyncRecord> _syncRecordsRepository;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public TrackingService(
        IGeneralRepository<Claim> claimsRepository,
        IGeneralRepository<SyncRecord> syncRecordsRepository,
        IMediator mediator,
        IMapper mapper
    )
    {
        _syncRecordsRepository = syncRecordsRepository;
        _claimsRepository = claimsRepository;
        _mediator = mediator;
        _mapper = mapper;
    }
    
    public async Task<TrackingModel?> GetTrackingByUuid(Guid uuid)
    {
        var claim = await _claimsRepository
            .All()
            .Include(o => o.ClaimantSyncRecord)
            .FirstOrDefaultAsync(o => o.UniversalId == uuid);

        if (claim is not null)
        {
            var tracking = await _mediator.Send(new GetDorothyOrderTrackingCommand(claim.UniversalId));

            return _mapper.Map<TrackingModel>(new ClaimTrackingModel(claim, tracking));
        }
        
        var syncRecord = await _syncRecordsRepository
            .All()
            .Include(o => o.Claims)
            .Include(o => o.SyncDatum)
            .FirstOrDefaultAsync(o => o.UniversalId == uuid);

        if (syncRecord is not null)
        {
            return _mapper.Map<TrackingModel>(syncRecord);
        }

        return null;
    }
}