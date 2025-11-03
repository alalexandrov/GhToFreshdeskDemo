using FluentAssertions;
using NSubstitute;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Sync;
using GhToFreshdesk.Domain.Users;

namespace GhToFreshdesk.Tests.Application.Sync;

public sealed class SyncGitHubUserHandler_NotFound_Tests
{
    [Fact]
    public async Task Throws_when_github_user_not_found()
    {
        const string tenant = "t";
        const string login  = "ghost";

        var ghClient = Substitute.For<IGitHubClient>();
        var fdClient = Substitute.For<IFreshdeskClient>();

        ghClient.GetUserAsync(login, Arg.Any<CancellationToken>())
            .Returns((GitHubUser?)null);

        var handler = new SyncGitHubUserHandler(ghClient, fdClient);
        
        var act = async () => await handler.Handle(new SyncGitHubUserCommand(tenant, login), CancellationToken.None);
        
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*ghost*");
        
        await fdClient.DidNotReceiveWithAnyArgs().FindContactIdByExternalIdAsync(default!, default!, default);
        await fdClient.DidNotReceiveWithAnyArgs().FindContactIdByEmailAsync(default!, default!, default);
        await fdClient.DidNotReceiveWithAnyArgs().CreateContactAsync(default!, default!, default);
        await fdClient.DidNotReceiveWithAnyArgs().UpdateContactAsync(default!, 0, default!, default);
    }
}