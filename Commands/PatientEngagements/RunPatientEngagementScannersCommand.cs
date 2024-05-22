using System;
using MediatR;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Commands.PatientEngagements;

public record RunPatientEngagementScannersCommand(DateTime UtcNow, EngagementAssignee Assignee) : IRequest;
