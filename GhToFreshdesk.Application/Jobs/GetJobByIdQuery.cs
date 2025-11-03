using MediatR;
using GhToFreshdesk.Application.Abstractions;

namespace GhToFreshdesk.Application.Jobs;

public sealed record GetJobByIdQuery(Guid JobId) : IRequest<JobDto?>;