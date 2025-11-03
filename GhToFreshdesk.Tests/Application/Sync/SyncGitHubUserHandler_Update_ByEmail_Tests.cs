using FluentAssertions;
using NSubstitute;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Sync;
using GhToFreshdesk.Domain.Users;
using GhToFreshdesk.Domain.Contacts;

namespace GhToFreshdesk.Tests.Application.Sync;

public sealed class SyncGitHubUserHandler_Update_ByEmail_Tests
{
    [Fact]
    public async Task Updates_contact_when_found_by_email()
    {
        const string tenant = "tenant-c";
        const string login  = "someone";
        const long ghId     = 777;
        const long foundId  = 123;

        var expectedExternalId = ExternalId.ForGitHub(ghId);

        var ghClient = Substitute.For<IGitHubClient>();
        var fdClient = Substitute.For<IFreshdeskClient>();

        var ghUser = new GitHubUser(
            Id: ghId,
            Login: login,
            Name: "Some One",
            Company: "Co",
            Email: "someone@example.com",
            Blog: null, Location: null, Bio: null, AvatarUrl: null
        );

        ghClient.GetUserAsync(login, Arg.Any<CancellationToken>()).Returns(ghUser);

        fdClient.FindContactIdByExternalIdAsync(tenant, expectedExternalId, Arg.Any<CancellationToken>())
                .Returns((long?)null);

        fdClient.FindContactIdByEmailAsync(tenant, ghUser.Email!, Arg.Any<CancellationToken>())
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