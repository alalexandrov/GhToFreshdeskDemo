using FluentAssertions;
using GhToFreshdesk.Application.Sync;

namespace GhToFreshdesk.Tests.Application.Sync;

public sealed class SyncGitHubUserCommandValidatorTests
{
    [Fact]
    public void Empty_tenant_and_login_fail()
    {
        var v = new SyncGitHubUserCommandValidator();

        var result = v.Validate(new SyncGitHubUserCommand("", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SyncGitHubUserCommand.Tenant));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SyncGitHubUserCommand.Login));
    }

    [Fact]
    public void Too_long_values_fail()
    {
        var longStr = new string('x', 201);
        var v = new SyncGitHubUserCommandValidator();

        var result = v.Validate(new SyncGitHubUserCommand(longStr, longStr));

        result.IsValid.Should().BeFalse();
        result.Errors.Count(e => e.PropertyName == nameof(SyncGitHubUserCommand.Tenant)).Should().BeGreaterThan(0);
        result.Errors.Count(e => e.PropertyName == nameof(SyncGitHubUserCommand.Login)).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Valid_values_pass()
    {
        var v = new SyncGitHubUserCommandValidator();

        var result = v.Validate(new SyncGitHubUserCommand("tenant-a", "octocat"));

        result.IsValid.Should().BeTrue();
    }
}