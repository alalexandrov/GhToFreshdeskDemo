using FluentAssertions;
using NSubstitute;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Sync;
using GhToFreshdesk.Domain.Users;
using GhToFreshdesk.Domain.Contacts;

namespace GhToFreshdesk.Tests.Application.Sync;

public sealed class SyncGitHubUserHandler_Update_ByExternalId_Tests
{
    [Fact]
    public async Task Updates_contact_when_found_by_external_id()
    {
        const string tenant = "tenant-b";
        const string login  = "alex";
        const long ghId     = 999;
        const long foundId  = 42;

        var expectedExternalId = ExternalId.ForGitHub(ghId);

        var ghClient = Substitute.For<IGitHubClient>();
        var fdClient = Substitute.For<IFreshdeskClient>();

        var ghUser = new GitHubUser(
            Id: ghId,
            Login: login,
            Name: "Alex",
            Company: "Company",
            Email: "alex@example.com",
            Blog: null, Location: null, Bio: null, AvatarUrl: null
        );

        ghClient.GetUserAsync(login, Arg.Any<CancellationToken>()).Returns(ghUser);

        fdClient.FindContactIdByExternalIdAsync(tenant, expectedExternalId, Arg.Any<CancellationToken>())
                .Returns(foundId);

        var handler = new SyncGitHubUserHandler(ghClient, fdClient);
        
        var result = await handler.Handle(new SyncGitHubUserCommand(tenant, login), CancellationToken.None);
        
        result.Action.Should().Be("updated");
        result.FreshdeskContactId.Should().Be(foundId);
        result.GitHubId.Should().Be(ghId);
        result.UniqueExternalId.Should().Be(expectedExternalId);

        await fdClient.Received(1).UpdateContactAsync(
            tenant,
            foundId,
            Arg.Is<FreshdeskContact>(c => c.UniqueExternalId == expectedExternalId
                                          && c.Email == ghUser.Email
                                          && c.Name == ghUser.Name),
            Arg.Any<CancellationToken>());

        await fdClient.DidNotReceive().CreateContactAsync(
            Arg.Any<string>(), Arg.Any<FreshdeskContact>(), Arg.Any<CancellationToken>());
    }
}