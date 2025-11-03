using FluentValidation;
using MediatR;

namespace GhToFreshdesk.Application.Jobs;

public sealed record EnqueueSyncGitHubUserCommand(string Tenant, string Login)
    : IRequest<EnqueueSyncGitHubUserResult>;

public sealed class EnqueueSyncGitHubUserCommandValidator : AbstractValidator<EnqueueSyncGitHubUserCommand>
{
    public EnqueueSyncGitHubUserCommandValidator()
    {
        RuleFor(x => x.Tenant).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(200);
    }
}

public sealed record EnqueueSyncGitHubUserResult(Guid JobId);