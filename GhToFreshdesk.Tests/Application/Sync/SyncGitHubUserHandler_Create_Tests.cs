using FluentAssertions;
using NSubstitute;
using GhToFreshdesk.Application.Abstractions;
using GhToFreshdesk.Application.Sync;
using GhToFreshdesk.Domain.Users;
using GhToFreshdesk.Domain.Contacts;

namespace GhToFreshdesk.Tests.Application.Sync;

public sealed class SyncGitHubUserHandler_Create_Tests
{
    [Fact]
    public async Task Creates_contact_when_not_found_by_external_id_or_email()
    {
        const string tenant = "tenant-a";
        const string login = "octocat";
        const long ghId = 12345;
        const long createdFreshdeskId = 205001681783;
        var expectedExternalId = ExternalId.ForGitHub(ghId); // "github:12345"

        var ghClient = Substitute.For<IGitHubClient>();
        var fdClient = Substitute.For<IFreshdeskClient>();

        var ghUser = new GitHubUser(
            Id: ghId,
            Login: login,
            Name: "Octo Cat",
            Company: "GitHub",
            Email: "octocat@example.com",
            Blog: "https://blog.example",
            Location: "Internet",
            Bio: "Mascot",
            AvatarUrl: "https://avatars.example/octocat.png"
        );

        ghClient.GetUserAsync(login, Arg.Any<CancellationToken>())
                .Returns(ghUser);

        fdClient.FindContactIdByExternalIdAsync(tenant, expectedExternalId, Arg.Any<CancellationToken>())
                .Returns((long?)null);
        
        fdClient.FindContactIdByEmailAsync(tenant, ghUser.Email!, Arg.Any<CancellationToken>())
                .Returns((long?)null);

        fdClient.CreateContactAsync(tenant, Arg.Any<FreshdeskContact>(), Arg.Any<CancellationToken>())
                .Returns(createdFreshdeskId);

        var handler = new SyncGitHubUserHandler(ghClient, fdClient);
        
        var result = await handler.Handle(new SyncGitHubUserCommand(tenant, login), CancellationToken.None);
        
        result.Action.Should().Be("created");
        result.FreshdeskContactId.Should().Be(createdFreshdeskId);
        result.GitHubId.Should().Be(ghId);
        result.UniqueExternalId.Should().Be(expectedExternalId);

        await fdClient.Received(1).CreateContactAsync(
            tenant,
            Arg.Is<FreshdeskContact>(c => c.UniqueExternalId == expectedExternalId
                                          && c.Email == ghUser.Email
                                          && c.Name == ghUser.Name),
            Arg.Any<CancellationToken>());

        await fdClient.DidNotReceive().UpdateContactAsync(
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<FreshdeskContact>(), Arg.Any<CancellationToken>());
    }
}