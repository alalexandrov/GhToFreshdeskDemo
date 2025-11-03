using FluentValidation;
using MediatR;

namespace GhToFreshdesk.Application.Jobs;

public sealed record RetryJobCommand(Guid JobId) : IRequest<bool>;

public sealed class RetryJobCommandValidator : AbstractValidator<RetryJobCommand>
{
    public RetryJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("JobId must not be empty.");
    }
}