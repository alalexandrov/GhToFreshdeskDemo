using FluentAssertions;
using GhToFreshdesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GhToFreshdesk.Tests.Infrastructure;

public class EfJobStoreTests
{
    private static EfJobStore NewStore(AppDbContext db) => new(db);

    [Fact]
    public async Task Enqueue_dedupes_when_open_job_exists()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id1 = await store.EnqueueSyncGithubUserAsync("tenant-a", "octocat");
        var id2 = await store.EnqueueSyncGithubUserAsync("tenant-a", "octocat");

        id2.Should().Be(id1, "same open job should be returned");
        (await db.Jobs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Enqueue_after_succeeded_resets_same_row()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("tenant-a", "octocat");
        await store.MarkSucceededAsync(id);

        var idAgain = await store.EnqueueSyncGithubUserAsync("tenant-a", "octocat");

        idAgain.Should().Be(id, "row is reused (unique correlation key)");
        var job = await db.Jobs.FirstAsync(j => j.Id == id);
        job.Status.ToString().Should().Be("Pending");
        job.Attempts.Should().Be(0);
        job.LastError.Should().BeNull();
        job.PickedAt.Should().BeNull();
        job.WorkerId.Should().BeNull();
    }

    [Fact]
    public async Task TryClaimNextDue_returns_null_when_no_jobs()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var claimed = await store.TryClaimNextDueAsync("worker-1");
        claimed.Should().BeNull();
    }

    [Fact]
    public async Task TryClaimNextDue_claims_pending_due_job()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");
        var claimed = await store.TryClaimNextDueAsync("w1");

        claimed.Should().NotBeNull();
        claimed!.Id.Should().Be(id);
        claimed.Status.ToString().Should().Be("Processing");

        var row = await db.Jobs.FirstAsync(j => j.Id == id);
        row.Status.ToString().Should().Be("Processing");
        row.WorkerId.Should().Be("w1");
        row.PickedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduleRetry_advances_AvailableAt_and_increments_Attempts()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");

        await store.ScheduleRetryAsync(id, "boom", TimeSpan.FromSeconds(3));
        var job = await db.Jobs.FirstAsync(j => j.Id == id);

        job.Attempts.Should().Be(1);
        job.Status.ToString().Should().Be("Pending");
        job.AvailableAt.Should().BeOnOrAfter(DateTime.UtcNow.AddSeconds(2)); // allow small timing skew
        job.LastError.Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduleRetry_moves_to_DeadLetter_after_MaxAttempts()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");
        
        for (int i = 0; i < 5; i++)
            await store.ScheduleRetryAsync(id, "boom", TimeSpan.FromMilliseconds(1));

        var job = await db.Jobs.FirstAsync(j => j.Id == id);
        job.Attempts.Should().BeGreaterThanOrEqualTo(job.MaxAttempts);
        job.Status.ToString().Should().Be("DeadLetter");
        job.LastError.Should().NotBeNull();
    }

    [Fact]
    public async Task Retry_resets_to_Pending_now_and_clears_error()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");
        await store.ScheduleRetryAsync(id, "err", TimeSpan.FromSeconds(10));

        await store.RetryAsync(id);

        var job = await db.Jobs.FirstAsync(j => j.Id == id);
        job.Status.ToString().Should().Be("Pending");
        job.Attempts.Should().Be(0);
        job.LastError.Should().BeNull();
        job.WorkerId.Should().BeNull();
        job.PickedAt.Should().BeNull();
        job.AvailableAt.Should().BeOnOrAfter(DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetById_returns_dto_when_exists_else_null()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");

        var dto = await store.GetByIdAsync(id);
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(id);
        dto.Tenant.Should().Be("t");
        dto.Login.Should().Be("u");

        var missing = await store.GetByIdAsync(Guid.NewGuid());
        missing.Should().BeNull();
    }

    [Fact]
    public async Task TryClaimNextDue_only_claims_when_AvailableAt_due()
    {
        using var db = TestDb.NewContext();
        var store = NewStore(db);

        var id = await store.EnqueueSyncGithubUserAsync("t", "u");
        
        var job = await db.Jobs.FirstAsync(j => j.Id == id);
        job.AvailableAt = DateTime.UtcNow.AddSeconds(5);
        await db.SaveChangesAsync();

        var none = await store.TryClaimNextDueAsync("w1");
        none.Should().BeNull();
        
        job.AvailableAt = DateTime.UtcNow.AddMilliseconds(-100);
        await db.SaveChangesAsync();

        var claimed = await store.TryClaimNextDueAsync("w1");
        claimed.Should().NotBeNull();
        claimed!.Id.Should().Be(id);
    }
}