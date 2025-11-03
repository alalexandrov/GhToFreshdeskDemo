using FluentValidation;
using MediatR;

namespace GhToFreshdesk.Application.Sync;

public sealed record SyncGitHubUserCommand(string Tenant, string Login) : IRequest<SyncGitHubUserResult>;

public sealed class SyncGitHubUserCommandValidator : AbstractValidator<SyncGitHubUserCommand>
{
    public SyncGitHubUserCommandValidator()
    {
        RuleFor(x => x.Tenant).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Login).NotEmpty().MaximumLength(200);
    }
}

public sealed record SyncGitHubUserResult(
    string Action,
    long FreshdeskContactId,
    string UniqueExternalId,
    long GitHubId
);